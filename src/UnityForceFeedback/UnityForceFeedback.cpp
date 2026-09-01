// UnityForceFeedback.dll — the native plugin art of rally expects but never shipped.
//
// The game's managed `ForceFeedback` MonoBehaviour P/Invokes these seven entry
// points (module name "UnityForceFeedback", CallingConvention.Winapi). The DLL
// is absent from the shipped build, so every FFB call fails and the game's
// force feedback — which is otherwise fully written and wired to CarDynamics —
// is dead. Supplying this file revives it without patching a single line of
// game code. See docs/FORCE-FEEDBACK.md.
//
// Contract, read out of Assembly-CSharp.dll metadata (docs/FINDINGS.md):
//     int  InitDirectInput(int hwnd);
//     void Aquire(void);                  // sic - the game's spelling
//     int  SetDeviceForcesXY(int x, int y);
//     BOOL StartEffect(void);
//     BOOL StopEffect(void);
//     BOOL SetAutoCenter(BOOL enable);
//     void FreeDirectInput(void);
//
// Returns are 4-byte Win32 BOOL, NOT C++ bool: the default P/Invoke marshalling
// for `bool` is 4 bytes. Returning a 1-byte C++ bool leaves the upper 3 bytes
// undefined and the managed side reads garbage.
//
// Set AOSR_FFB_LOG=1 to trace every call to %LOCALAPPDATA%\ArtOfSimRally\ffb.log.
// That turns this DLL into the probe described in docs/ROADMAP.md phase 0: if
// the log shows InitDirectInput followed by a stream of SetDeviceForcesXY during
// a stage, the whole premise is proven.

#define DIRECTINPUT_VERSION 0x0800
#define WIN32_LEAN_AND_MEAN

#include <windows.h>
#include <dinput.h>
#include <shlobj.h>
#include <cstdio>
#include <cstdarg>
#include <cstring>

#pragma comment(lib, "dinput8.lib")
#pragma comment(lib, "dxguid.lib")
#pragma comment(lib, "shell32.lib")
#pragma comment(lib, "ole32.lib")
#pragma comment(lib, "user32.lib")

// DirectInput's nominal full-scale force. The game hands us values it has
// already scaled and clamped (ForceFeedback.clampValue), but never trust it.
static const int  kForceMax = DI_FFNOMINALMAX;   // 10000
static const char kLogEnv[] = "AOSR_FFB_LOG";

static LPDIRECTINPUT8       g_di      = nullptr;
static LPDIRECTINPUTDEVICE8 g_device  = nullptr;
static LPDIRECTINPUTEFFECT  g_effect  = nullptr;
static HWND                 g_hwnd    = nullptr;
static bool                 g_logging = false;
static bool                 g_logChecked = false;
static CRITICAL_SECTION     g_lock;
static bool                 g_lockInit = false;

// --------------------------------------------------------------------------
// logging
// --------------------------------------------------------------------------

// Logging is ON by default, and AOSR_FFB_LOG=0 turns it off.
//
// This is deliberately inverted from the obvious design. During phase 0 the
// question being asked is "does the game call this DLL at all", and an absent
// log file is the answer. If logging were opt-in, an absent log would be
// ambiguous between "the game never called us" and "the environment variable
// did not reach the game process" - two findings that point in opposite
// directions, and the second is easy to cause by accident (a Steam-launched
// game does not inherit a variable set in your shell). Defaulting to on
// collapses that ambiguity: no file means no call. The log is a few lines per
// session, so the cost is nil.
static bool LoggingEnabled()
{
    if (!g_logChecked) {
        char buf[8] = {};
        DWORD n = GetEnvironmentVariableA(kLogEnv, buf, sizeof(buf));
        g_logging = !(n > 0 && buf[0] == '0');
        g_logChecked = true;
    }
    return g_logging;
}

static void LogPath(char* out, size_t cch)
{
    PWSTR wide = nullptr;
    out[0] = '\0';
    if (SUCCEEDED(SHGetKnownFolderPath(FOLDERID_LocalAppData, 0, nullptr, &wide))) {
        char local[MAX_PATH] = {};
        WideCharToMultiByte(CP_UTF8, 0, wide, -1, local, MAX_PATH, nullptr, nullptr);
        CoTaskMemFree(wide);
        char dir[MAX_PATH] = {};
        _snprintf_s(dir, sizeof(dir), _TRUNCATE, "%s\\ArtOfSimRally", local);
        CreateDirectoryA(dir, nullptr);
        _snprintf_s(out, cch, _TRUNCATE, "%s\\ffb.log", dir);
    }
}

static void Log(const char* fmt, ...)
{
    if (!LoggingEnabled()) return;

    static char path[MAX_PATH] = {};
    if (path[0] == '\0') LogPath(path, sizeof(path));
    if (path[0] == '\0') return;

    FILE* f = nullptr;
    if (fopen_s(&f, path, "a") != 0 || !f) return;

    SYSTEMTIME st;
    GetLocalTime(&st);
    fprintf(f, "%02d:%02d:%02d.%03d  ", st.wHour, st.wMinute, st.wSecond, st.wMilliseconds);

    va_list args;
    va_start(args, fmt);
    vfprintf(f, fmt, args);
    va_end(args);

    fputc('\n', f);
    fclose(f);
}

// --------------------------------------------------------------------------
// device discovery
// --------------------------------------------------------------------------

// Prefer a wheel, but accept any force-feedback-capable game controller. Devices
// without FFB are skipped outright: acquiring one exclusively would steal input
// from Rewired for no benefit.
static BOOL CALLBACK EnumDeviceCallback(const DIDEVICEINSTANCE* inst, VOID* ctx)
{
    (void)ctx;

    LPDIRECTINPUTDEVICE8 candidate = nullptr;
    if (FAILED(g_di->CreateDevice(inst->guidInstance, &candidate, nullptr)))
        return DIENUM_CONTINUE;

    DIDEVCAPS caps = {};
    caps.dwSize = sizeof(caps);
    if (FAILED(candidate->GetCapabilities(&caps)) || !(caps.dwFlags & DIDC_FORCEFEEDBACK)) {
        Log("  skip (no FFB): %s", inst->tszProductName);
        candidate->Release();
        return DIENUM_CONTINUE;
    }

    Log("  using FFB device: %s (%u axes, %u buttons)",
        inst->tszProductName, caps.dwAxes, caps.dwButtons);

    g_device = candidate;
    return DIENUM_STOP;
}

// The game truncates GetForegroundWindow() into an int. HWND values fit in 32
// bits on Windows in practice, but sign-extend rather than zero-extend so a
// high-bit handle survives, and fall back to discovering the window ourselves.
static HWND ResolveHwnd(int hwnd)
{
    HWND h = (HWND)(INT_PTR)hwnd;
    if (h && IsWindow(h)) return h;

    h = GetForegroundWindow();
    if (h && IsWindow(h)) {
        Log("  hwnd %d invalid; using foreground window %p", hwnd, (void*)h);
        return h;
    }
    Log("  hwnd %d invalid and no foreground window", hwnd);
    return nullptr;
}

static void ReleaseEffect()
{
    if (g_effect) {
        g_effect->Stop();
        g_effect->Release();
        g_effect = nullptr;
    }
}

// --------------------------------------------------------------------------
// exports
// --------------------------------------------------------------------------

extern "C" {

// Forward-declared: InitDirectInput unwinds through it on every failure path.
__declspec(dllexport) void FreeDirectInput(void);

// Returns 1 on success, 0 on failure. The game ignores the value, but a
// non-zero result is the honest signal and makes the log readable.
__declspec(dllexport) int InitDirectInput(int hwnd)
{
    Log("InitDirectInput(hwnd=%d)", hwnd);

    if (!g_lockInit) { InitializeCriticalSection(&g_lock); g_lockInit = true; }

    if (g_device) {
        Log("  already initialised");
        return 1;
    }

    g_hwnd = ResolveHwnd(hwnd);
    if (!g_hwnd) return 0;

    HRESULT hr = DirectInput8Create(GetModuleHandle(nullptr), DIRECTINPUT_VERSION,
                                    IID_IDirectInput8, (VOID**)&g_di, nullptr);
    if (FAILED(hr)) { Log("  DirectInput8Create failed 0x%08lX", hr); return 0; }

    g_di->EnumDevices(DI8DEVCLASS_GAMECTRL, EnumDeviceCallback, nullptr, DIEDFL_ATTACHEDONLY);
    if (!g_device) {
        Log("  no force-feedback device found");
        g_di->Release(); g_di = nullptr;
        return 0;
    }

    if (FAILED(hr = g_device->SetDataFormat(&c_dfDIJoystick2))) {
        Log("  SetDataFormat failed 0x%08lX", hr);
        FreeDirectInput();
        return 0;
    }

    // Force feedback requires exclusive access. Background keeps effects alive
    // when the game loses focus, which matters on a multi-monitor sim rig.
    if (FAILED(hr = g_device->SetCooperativeLevel(g_hwnd, DISCL_EXCLUSIVE | DISCL_BACKGROUND))) {
        Log("  SetCooperativeLevel(EXCLUSIVE|BACKGROUND) failed 0x%08lX - "
            "another process may hold the wheel", hr);
        FreeDirectInput();
        return 0;
    }

    // Autocentre fights every effect we apply; off by default, the game can
    // turn it back on through SetAutoCenter.
    DIPROPDWORD prop = {};
    prop.diph.dwSize       = sizeof(DIPROPDWORD);
    prop.diph.dwHeaderSize = sizeof(DIPROPHEADER);
    prop.diph.dwObj        = 0;
    prop.diph.dwHow        = DIPH_DEVICE;
    prop.dwData            = DIPROPAUTOCENTER_OFF;
    g_device->SetProperty(DIPROP_AUTOCENTER, &prop.diph);

    g_device->Acquire();

    // A single constant force on X (+Y where the device has it). This mirrors
    // what the game asks for: SetDeviceForcesXY is the only force call it makes.
    DWORD axes[2]      = { DIJOFS_X, DIJOFS_Y };
    LONG  direction[2] = { 0, 0 };

    DICONSTANTFORCE constant = {};
    constant.lMagnitude = 0;

    DIEFFECT effect      = {};
    effect.dwSize        = sizeof(DIEFFECT);
    effect.dwFlags       = DIEFF_CARTESIAN | DIEFF_OBJECTOFFSETS;
    effect.dwDuration    = INFINITE;
    effect.dwGain        = DI_FFNOMINALMAX;
    effect.dwTriggerButton = DIEB_NOTRIGGER;
    effect.cAxes         = 2;
    effect.rgdwAxes      = axes;
    effect.rglDirection  = direction;
    effect.cbTypeSpecificParams = sizeof(DICONSTANTFORCE);
    effect.lpvTypeSpecificParams = &constant;

    hr = g_device->CreateEffect(GUID_ConstantForce, &effect, &g_effect, nullptr);
    if (FAILED(hr)) {
        // Some wheels expose only a single FFB axis; retry with X alone.
        Log("  2-axis CreateEffect failed 0x%08lX, retrying single axis", hr);
        effect.cAxes = 1;
        hr = g_device->CreateEffect(GUID_ConstantForce, &effect, &g_effect, nullptr);
    }
    if (FAILED(hr)) {
        Log("  CreateEffect(GUID_ConstantForce) failed 0x%08lX", hr);
        FreeDirectInput();
        return 0;
    }

    Log("  initialised OK");
    return 1;
}

__declspec(dllexport) void Aquire(void)
{
    if (!g_device) { Log("Aquire() with no device"); return; }
    HRESULT hr = g_device->Acquire();
    // S_FALSE simply means "already acquired" - not worth logging every frame.
    if (hr != S_FALSE) Log("Aquire() -> 0x%08lX", hr);
}

// Called every frame while driving. Deliberately quiet unless the magnitude
// actually changes, so the trace log stays readable instead of 60 lines/second.
__declspec(dllexport) int SetDeviceForcesXY(int x, int y)
{
    if (!g_effect) return 0;

    if (x >  kForceMax) x =  kForceMax;
    if (x < -kForceMax) x = -kForceMax;
    if (y >  kForceMax) y =  kForceMax;
    if (y < -kForceMax) y = -kForceMax;

    static int lastX = INT_MIN, lastY = INT_MIN;
    if (x != lastX || y != lastY) {
        Log("SetDeviceForcesXY(%d, %d)", x, y);
        lastX = x; lastY = y;
    }

    EnterCriticalSection(&g_lock);

    LONG direction[2] = { (LONG)x, (LONG)y };
    DICONSTANTFORCE constant = {};
    // Cartesian direction carries the sign; magnitude is the vector length the
    // device should pull with. Using |x| keeps a pure-X setup behaving exactly
    // as the game intends: negative x pulls one way, positive the other.
    constant.lMagnitude = (LONG)x;

    DIEFFECT update      = {};
    update.dwSize        = sizeof(DIEFFECT);
    update.dwFlags       = DIEFF_CARTESIAN | DIEFF_OBJECTOFFSETS;
    update.cAxes         = 2;
    update.rglDirection  = direction;
    update.cbTypeSpecificParams  = sizeof(DICONSTANTFORCE);
    update.lpvTypeSpecificParams = &constant;

    HRESULT hr = g_effect->SetParameters(
        &update, DIEP_DIRECTION | DIEP_TYPESPECIFICPARAMS | DIEP_START);

    LeaveCriticalSection(&g_lock);

    if (hr == DIERR_INPUTLOST || hr == DIERR_NOTACQUIRED) {
        g_device->Acquire();
        return 0;
    }
    return SUCCEEDED(hr) ? 1 : 0;
}

__declspec(dllexport) BOOL StartEffect(void)
{
    Log("StartEffect()");
    if (!g_effect) return FALSE;
    return SUCCEEDED(g_effect->Start(1, 0)) ? TRUE : FALSE;
}

__declspec(dllexport) BOOL StopEffect(void)
{
    Log("StopEffect()");
    if (!g_effect) return FALSE;
    return SUCCEEDED(g_effect->Stop()) ? TRUE : FALSE;
}

__declspec(dllexport) BOOL SetAutoCenter(BOOL enable)
{
    Log("SetAutoCenter(%d)", enable);
    if (!g_device) return FALSE;

    DIPROPDWORD prop = {};
    prop.diph.dwSize       = sizeof(DIPROPDWORD);
    prop.diph.dwHeaderSize = sizeof(DIPROPHEADER);
    prop.diph.dwObj        = 0;
    prop.diph.dwHow        = DIPH_DEVICE;
    prop.dwData            = enable ? DIPROPAUTOCENTER_ON : DIPROPAUTOCENTER_OFF;

    return SUCCEEDED(g_device->SetProperty(DIPROP_AUTOCENTER, &prop.diph)) ? TRUE : FALSE;
}

__declspec(dllexport) void FreeDirectInput(void)
{
    Log("FreeDirectInput()");
    ReleaseEffect();
    if (g_device) { g_device->Unacquire(); g_device->Release(); g_device = nullptr; }
    if (g_di)     { g_di->Release();  g_di = nullptr; }
}

} // extern "C"

BOOL APIENTRY DllMain(HMODULE, DWORD reason, LPVOID)
{
    // The load event is the single most informative line in the log. P/Invoke
    // binds lazily, so this fires only when the game actually calls one of our
    // exports - which means "DLL loaded" is direct proof that the game's
    // ForceFeedback MonoBehaviour is alive and reaching for the native side.
    if (reason == DLL_PROCESS_ATTACH) {
        Log("=== UnityForceFeedback.dll loaded into pid %lu ===", GetCurrentProcessId());
    }
    if (reason == DLL_PROCESS_DETACH && g_lockInit) {
        DeleteCriticalSection(&g_lock);
        g_lockInit = false;
    }
    return TRUE;
}
