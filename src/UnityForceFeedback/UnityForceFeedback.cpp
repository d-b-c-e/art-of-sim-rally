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
#include <cctype>

#pragma comment(lib, "dinput8.lib")
#pragma comment(lib, "dxguid.lib")
#pragma comment(lib, "shell32.lib")
#pragma comment(lib, "ole32.lib")
#pragma comment(lib, "user32.lib")

// DirectInput's nominal full-scale force. The game hands us values it has
// already scaled and clamped (ForceFeedback.clampValue), but never trust it.
static const int  kForceMax = DI_FFNOMINALMAX;   // 10000
static const char kLogEnv[] = "AOSR_FFB_LOG";

// Enough for any realistic rig; more FFB devices than this and the user has
// bigger problems than picking one.
static const int kMaxCandidates = 8;

struct Candidate { GUID guid; char name[MAX_PATH]; };
static Candidate g_candidates[kMaxCandidates];
static int       g_candidateCount = 0;
static char      g_preferred[MAX_PATH] = {};
static int       g_preferredIndex = -1;

// Axis count the constant-force effect was actually created with. Every
// SetParameters call must agree with it.
static DWORD     g_effectAxes = 2;
static GUID      g_activeGuid = {};      // instance GUID of the wheel we hold
static char      g_activeName[260] = {};

// A second device we read directly - typically a sequential or H-pattern
// shifter. Kept entirely separate from the force feedback device: it is opened
// non-exclusively so it never competes with the game's own input, and it exists
// precisely because Rewired's Raw Input backend does not enumerate these at all.
static const int kMaxAuxDevices = 16;
struct AuxDevice { GUID guid; char name[MAX_PATH]; };
static AuxDevice g_aux[kMaxAuxDevices];
static int       g_auxCount = 0;
static LPDIRECTINPUTDEVICE8 g_auxDevice = nullptr;

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

// Case-insensitive substring test, for matching a user-supplied device name
// against DirectInput's product string.
static bool ContainsNoCase(const char* haystack, const char* needle)
{
    if (!haystack || !needle || !*needle) return false;
    size_t hn = strlen(haystack), nn = strlen(needle);
    if (nn > hn) return false;
    for (size_t i = 0; i + nn <= hn; ++i) {
        size_t j = 0;
        while (j < nn && tolower((unsigned char)haystack[i + j]) ==
                         tolower((unsigned char)needle[j])) ++j;
        if (j == nn) return true;
    }
    return false;
}

// Enumerates every force-feedback-capable controller rather than stopping at the
// first. Two reasons: a rig can easily have more than one FFB device (a wheel
// plus an FFB joystick, say) and silently grabbing whichever DirectInput happened
// to list first is a coin flip; and logging all of them turns "force feedback
// didn't work" into a diagnosable report of what was actually present.
//
// Devices without FFB are skipped outright - acquiring one exclusively would take
// input away from Rewired for no benefit.
static BOOL CALLBACK EnumDeviceCallback(const DIDEVICEINSTANCE* inst, VOID* ctx)
{
    (void)ctx;
    if (g_candidateCount >= kMaxCandidates) return DIENUM_STOP;

    // The wheel we already hold exclusively is not re-opened to be inspected:
    // a second device object on a live FFB device is the one interaction in
    // the log immediately before every update started failing on a MOZA
    // R12. Its answer is known - it is an FFB device - so it is listed
    // without touching it.
    if (g_device && IsEqualGUID(inst->guidInstance, g_activeGuid)) {
        Candidate& held = g_candidates[g_candidateCount++];
        held.guid = inst->guidInstance;
        strncpy_s(held.name, sizeof(held.name), inst->tszProductName, _TRUNCATE);
        Log("  found FFB device [%d]: %s (in use)", g_candidateCount - 1, held.name);
        return DIENUM_CONTINUE;
    }

    LPDIRECTINPUTDEVICE8 candidate = nullptr;
    if (FAILED(g_di->CreateDevice(inst->guidInstance, &candidate, nullptr)))
        return DIENUM_CONTINUE;

    DIDEVCAPS caps = {};
    caps.dwSize = sizeof(caps);
    if (FAILED(candidate->GetCapabilities(&caps)) || !(caps.dwFlags & DIDC_FORCEFEEDBACK)) {
        Log("  skip (no force feedback): %s", inst->tszProductName);
        candidate->Release();
        return DIENUM_CONTINUE;
    }

    Candidate& c = g_candidates[g_candidateCount++];
    c.guid = inst->guidInstance;
    strncpy_s(c.name, sizeof(c.name), inst->tszProductName, _TRUNCATE);
    Log("  found FFB device [%d]: %s (%u axes, %u buttons)",
        g_candidateCount - 1, c.name, caps.dwAxes, caps.dwButtons);

    candidate->Release();
    return DIENUM_CONTINUE;
}

// Collects every attached game controller, force feedback or not. The FFB
// enumeration deliberately skips devices without actuators; this one must not,
// because a shifter is exactly such a device.
static BOOL CALLBACK EnumAuxCallback(const DIDEVICEINSTANCE* inst, VOID* ctx)
{
    (void)ctx;
    if (g_auxCount >= kMaxAuxDevices) return DIENUM_STOP;

    AuxDevice& d = g_aux[g_auxCount++];
    d.guid = inst->guidInstance;
    strncpy_s(d.name, sizeof(d.name), inst->tszProductName, _TRUNCATE);
    Log("  device [%d]: %s", g_auxCount - 1, d.name);
    return DIENUM_CONTINUE;
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

// The window the exclusive cooperative level is bound to must belong to this
// process. The caller passes GetForegroundWindow(), which at init time can be
// something else entirely (an overlay, a console). Find our own top-level
// window instead: visible, unowned, largest.
struct OwnWindowSearch { HWND best; long area; };
static BOOL CALLBACK OwnWindowCb(HWND w, LPARAM lp)
{
    DWORD pid = 0; GetWindowThreadProcessId(w, &pid);
    if (pid != GetCurrentProcessId() || !IsWindowVisible(w) || GetWindow(w, GW_OWNER)) return TRUE;
    RECT r; if (!GetWindowRect(w, &r)) return TRUE;
    long area = (long)(r.right - r.left) * (long)(r.bottom - r.top);
    OwnWindowSearch* s = (OwnWindowSearch*)lp;
    if (area > s->area) { s->area = area; s->best = w; }
    return TRUE;
}
static HWND ResolveGameWindow(HWND passed)
{
    DWORD pid = 0;
    if (passed && IsWindow(passed)) { GetWindowThreadProcessId(passed, &pid); if (pid == GetCurrentProcessId()) return passed; }
    OwnWindowSearch s = { nullptr, 0 };
    EnumWindows(OwnWindowCb, (LPARAM)&s);
    return s.best ? s.best : passed;
}

// Creates the constant-force effect on g_device. Separate from init so a
// dead effect can be rebuilt mid-session - see SetDeviceForcesXY.
static HRESULT CreateForceEffect()
{
    HRESULT hr;
    // A single constant force on X (+Y where the device has it). This mirrors
    // what the game asks for: SetDeviceForcesXY is the only force call it makes.
    DWORD axes[2]      = { DIJOFS_X, DIJOFS_Y };
    LONG  direction[2] = { 0, 0 };   // zero at rest; updates keep |direction| == |magnitude|

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

    g_effectAxes = 2;
    hr = g_device->CreateEffect(GUID_ConstantForce, &effect, &g_effect, nullptr);
    if (FAILED(hr)) {
        // Plenty of wheels expose a single FFB axis. Retry with X alone, and
        // remember that we did - SetParameters must describe the same number of
        // axes the effect was created with, or every update is rejected with
        // E_INVALIDARG and the wheel goes silently dead.
        Log("  2-axis CreateEffect failed 0x%08lX, retrying single axis", hr);
        effect.cAxes = 1;
        hr = g_device->CreateEffect(GUID_ConstantForce, &effect, &g_effect, nullptr);
        if (SUCCEEDED(hr)) g_effectAxes = 1;
    }
    return hr;
}

__declspec(dllexport) int InitDirectInput(int hwnd)
{
    Log("InitDirectInput(hwnd=%d)", hwnd);
    g_hwnd = ResolveGameWindow((HWND)(INT_PTR)hwnd);
    if (g_hwnd != (HWND)(INT_PTR)hwnd) Log("  using this process's own window %p instead of %p", (void*)g_hwnd, (void*)(INT_PTR)hwnd);

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

    g_candidateCount = 0;
    g_di->EnumDevices(DI8DEVCLASS_GAMECTRL, EnumDeviceCallback, nullptr, DIEDFL_ATTACHEDONLY);

    if (g_candidateCount == 0) {
        Log("  no force-feedback device found");
        g_di->Release(); g_di = nullptr;
        return 0;
    }

    // Honour a preferred name if one was set and matches; otherwise take the
    // first. Choosing explicitly beats whatever order DirectInput enumerated in.
    int chosen = 0;
    if (g_preferredIndex >= 0) {
        if (g_preferredIndex < g_candidateCount) {
            chosen = g_preferredIndex;
            Log("  using index %d as requested", chosen);
        } else {
            Log("  requested index %d but only %d FFB device(s) present; using [0]",
                g_preferredIndex, g_candidateCount);
        }
    }
    else if (g_preferred[0]) {
        bool matched = false;
        for (int i = 0; i < g_candidateCount; ++i) {
            if (ContainsNoCase(g_candidates[i].name, g_preferred)) {
                chosen = i; matched = true; break;
            }
        }
        if (!matched)
            Log("  preferred device '%s' not found among %d FFB device(s); using the first",
                g_preferred, g_candidateCount);
    }
    else if (g_candidateCount > 1) {
        Log("  %d FFB devices present and no preference set - using [0] '%s'. "
            "Set the preferred wheel in the mod settings if this is wrong.",
            g_candidateCount, g_candidates[0].name);
    }

    Log("  using: %s", g_candidates[chosen].name);

    if (FAILED(g_di->CreateDevice(g_candidates[chosen].guid, &g_device, nullptr)) || !g_device) {
        Log("  could not open the chosen device");
        g_di->Release(); g_di = nullptr;
        return 0;
    }
    g_activeGuid = g_candidates[chosen].guid;
    strncpy_s(g_activeName, sizeof(g_activeName), g_candidates[chosen].name, _TRUNCATE);

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
    // Axes in a known range, so the direct-input reader can calibrate against
    // 0..65535 on every device. Harmless for force feedback.
    {
        DIPROPRANGE range = {};
        range.diph.dwSize       = sizeof(DIPROPRANGE);
        range.diph.dwHeaderSize = sizeof(DIPROPHEADER);
        range.diph.dwObj        = 0;
        range.diph.dwHow        = DIPH_DEVICE;
        range.lMin = 0; range.lMax = 65535;
        g_device->SetProperty(DIPROP_RANGE, &range.diph);
    }

    g_device->Acquire();

    hr = CreateForceEffect();
    if (FAILED(hr)) {
        Log("  CreateEffect(GUID_ConstantForce) failed 0x%08lX", hr);
        FreeDirectInput();
        return 0;
    }
    Log("  initialised OK (constant force on %lu axis/axes)", g_effectAxes);
    return 1;
}

// Enumerates force-feedback devices without opening or acquiring anything, so
// the settings UI can show the user a list of real device names to pick from
// instead of asking for an index. Safe to call at any time.
__declspec(dllexport) int EnumerateDevices(void)
{
    if (!g_lockInit) { InitializeCriticalSection(&g_lock); g_lockInit = true; }

    bool temporary = (g_di == nullptr);
    if (temporary) {
        HRESULT hr = DirectInput8Create(GetModuleHandle(nullptr), DIRECTINPUT_VERSION,
                                        IID_IDirectInput8, (VOID**)&g_di, nullptr);
        if (FAILED(hr)) { Log("EnumerateDevices: DirectInput8Create failed 0x%08lX", hr); return 0; }
    }

    g_candidateCount = 0;
    g_di->EnumDevices(DI8DEVCLASS_GAMECTRL, EnumDeviceCallback, nullptr, DIEDFL_ATTACHEDONLY);

    // Only release what we created here; never tear down a live session.
    if (temporary && !g_device) { g_di->Release(); g_di = nullptr; }

    return g_candidateCount;
}

// Copies the name of an enumerated device. Returns 0 if the index is out of
// range, so the caller can stop without knowing the count in advance.
__declspec(dllexport) int GetDeviceName(int index, char* buffer, int size)
{
    if (!buffer || size <= 0) return 0;
    buffer[0] = 0;
    if (index < 0 || index >= g_candidateCount) return 0;
    strncpy_s(buffer, (size_t)size, g_candidates[index].name, _TRUNCATE);
    return 1;
}

// Not part of the game's original contract - an eighth export the mod calls
// before InitDirectInput. Harmless to the game, which never calls it.
__declspec(dllexport) void SetPreferredDevice(const char* name)
{
    if (!name || !*name) { g_preferred[0] = 0; return; }
    strncpy_s(g_preferred, sizeof(g_preferred), name, _TRUNCATE);
    Log("SetPreferredDevice(\"%s\")", g_preferred);
}

// Selects by enumeration index. Necessary because product names are not unique -
// a Fanatec rig reports two devices both called "FANATEC Wheel", which no name
// filter can tell apart.
__declspec(dllexport) void SetPreferredDeviceIndex(int index)
{
    g_preferredIndex = index;
    Log("SetPreferredDeviceIndex(%d)", index);
}

// ---- auxiliary (button-only) device: shifters, handbrakes -----------------

__declspec(dllexport) int EnumerateAllDevices(void)
{
    if (!g_lockInit) { InitializeCriticalSection(&g_lock); g_lockInit = true; }

    bool temporary = (g_di == nullptr);
    if (temporary) {
        HRESULT hr = DirectInput8Create(GetModuleHandle(nullptr), DIRECTINPUT_VERSION,
                                        IID_IDirectInput8, (VOID**)&g_di, nullptr);
        if (FAILED(hr)) { Log("EnumerateAllDevices failed 0x%08lX", hr); return 0; }
    }

    Log("EnumerateAllDevices()");
    g_auxCount = 0;
    g_di->EnumDevices(DI8DEVCLASS_GAMECTRL, EnumAuxCallback, nullptr, DIEDFL_ATTACHEDONLY);

    if (temporary && !g_device && !g_auxDevice) { g_di->Release(); g_di = nullptr; }
    return g_auxCount;
}

__declspec(dllexport) int GetAnyDeviceName(int index, char* buffer, int size)
{
    if (!buffer || size <= 0) return 0;
    buffer[0] = 0;
    if (index < 0 || index >= g_auxCount) return 0;
    strncpy_s(buffer, (size_t)size, g_aux[index].name, _TRUNCATE);
    return 1;
}

// Opened NON-exclusively and in the background: we only read buttons, and taking
// it exclusively would stop anything else - including the game - from seeing it.
__declspec(dllexport) int OpenAuxDevice(int index)
{
    // Enumerate if we have not yet, rather than only when DirectInput itself is
    // missing. Force feedback initialises first and leaves g_di set, so keying off
    // that skipped enumeration entirely and every open failed as "out of range".
    if (g_auxCount == 0) { if (EnumerateAllDevices() <= 0) return 0; }

    if (index < 0 || index >= g_auxCount) {
        Log("OpenAuxDevice(%d): out of range (%d devices)", index, g_auxCount);
        return 0;
    }

    if (g_auxDevice) { g_auxDevice->Unacquire(); g_auxDevice->Release(); g_auxDevice = nullptr; }

    HRESULT hr = g_di->CreateDevice(g_aux[index].guid, &g_auxDevice, nullptr);
    if (FAILED(hr) || !g_auxDevice) { Log("OpenAuxDevice: CreateDevice failed 0x%08lX", hr); return 0; }

    if (FAILED(hr = g_auxDevice->SetDataFormat(&c_dfDIJoystick2))) {
        Log("OpenAuxDevice: SetDataFormat failed 0x%08lX", hr);
        g_auxDevice->Release(); g_auxDevice = nullptr; return 0;
    }

    HWND hwnd = g_hwnd ? g_hwnd : GetForegroundWindow();
    if (FAILED(hr = g_auxDevice->SetCooperativeLevel(hwnd, DISCL_NONEXCLUSIVE | DISCL_BACKGROUND))) {
        Log("OpenAuxDevice: SetCooperativeLevel failed 0x%08lX", hr);
        g_auxDevice->Release(); g_auxDevice = nullptr; return 0;
    }

    g_auxDevice->Acquire();
    Log("OpenAuxDevice(%d): %s opened", index, g_aux[index].name);
    return 1;
}

// Fills one byte per button, 1 = pressed. Returns how many were written.
__declspec(dllexport) int ReadAuxButtons(unsigned char* buffer, int length)
{
    if (!g_auxDevice || !buffer || length <= 0) return 0;

    DIJOYSTATE2 state = {};
    HRESULT hr = g_auxDevice->Poll();
    if (FAILED(hr)) { g_auxDevice->Acquire(); g_auxDevice->Poll(); }

    hr = g_auxDevice->GetDeviceState(sizeof(state), &state);
    if (FAILED(hr)) {
        if (hr == DIERR_INPUTLOST || hr == DIERR_NOTACQUIRED) g_auxDevice->Acquire();
        return 0;
    }

    int n = length < 128 ? length : 128;
    for (int i = 0; i < n; ++i) buffer[i] = (state.rgbButtons[i] & 0x80) ? 1 : 0;
    return n;
}

__declspec(dllexport) void CloseAuxDevice(void)
{
    if (!g_auxDevice) return;
    g_auxDevice->Unacquire();
    g_auxDevice->Release();
    g_auxDevice = nullptr;
    Log("CloseAuxDevice()");
}

// --- direct reading of any controller's axes and buttons ----------------------
// Used for wheels the game's input library cannot read (Fanatec bases under
// Rewired's Raw Input). Every controller is opened non-exclusively for reading,
// except the force-feedback wheel, which is read through the exclusive handle we
// already hold - a second instance of the same device is not needed.
static const int kMaxReadSlots = 8;
struct ReadSlot { LPDIRECTINPUTDEVICE8 dev; bool isWheel; char name[MAX_PATH]; };
static ReadSlot g_read[kMaxReadSlots];
static int      g_readCount = 0;

static void SetFullRange(LPDIRECTINPUTDEVICE8 dev)
{
    DIPROPRANGE range = {};
    range.diph.dwSize       = sizeof(DIPROPRANGE);
    range.diph.dwHeaderSize = sizeof(DIPROPHEADER);
    range.diph.dwObj        = 0;
    range.diph.dwHow        = DIPH_DEVICE;
    range.lMin = 0; range.lMax = 65535;
    HRESULT hr = dev->SetProperty(DIPROP_RANGE, &range.diph);
    if (FAILED(hr)) Log("  DIPROP_RANGE 0..65535 refused 0x%08lX (device keeps its native range)", hr);
}

// Returns the slot to read from, or -1.
__declspec(dllexport) int OpenReadDevice(int index)
{
    if (g_auxCount == 0) { if (EnumerateAllDevices() <= 0) return -1; }
    if (index < 0 || index >= g_auxCount) { Log("OpenReadDevice(%d): out of range (%d devices)", index, g_auxCount); return -1; }
    if (g_readCount >= kMaxReadSlots) { Log("OpenReadDevice(%d): no free slot", index); return -1; }
    ReadSlot& s = g_read[g_readCount];
    s.dev = nullptr; s.isWheel = false;
    strncpy_s(s.name, sizeof(s.name), g_aux[index].name, _TRUNCATE);
    if (g_device && IsEqualGUID(g_aux[index].guid, g_activeGuid)) {
        s.isWheel = true;
        Log("OpenReadDevice(%d): %s is the force feedback wheel - read through that handle (slot %d)", index, s.name, g_readCount);
        return g_readCount++;
    }
    HRESULT hr = g_di->CreateDevice(g_aux[index].guid, &s.dev, nullptr);
    if (FAILED(hr) || !s.dev) { Log("OpenReadDevice(%d): CreateDevice failed 0x%08lX", index, hr); s.dev = nullptr; return -1; }
    if (FAILED(hr = s.dev->SetDataFormat(&c_dfDIJoystick2))) {
        Log("OpenReadDevice(%d): SetDataFormat failed 0x%08lX", index, hr);
        s.dev->Release(); s.dev = nullptr; return -1;
    }
    HWND hwnd = g_hwnd ? g_hwnd : GetForegroundWindow();
    if (FAILED(hr = s.dev->SetCooperativeLevel(hwnd, DISCL_NONEXCLUSIVE | DISCL_BACKGROUND))) {
        Log("OpenReadDevice(%d): SetCooperativeLevel failed 0x%08lX", index, hr);
        s.dev->Release(); s.dev = nullptr; return -1;
    }
    SetFullRange(s.dev);
    s.dev->Acquire();
    Log("OpenReadDevice(%d): %s opened for reading (slot %d)", index, s.name, g_readCount);
    return g_readCount++;
}

// axes: 8 ints (X Y Z Rx Ry Rz Slider1 Slider2); buttons: one byte each, 1 = pressed.
// Returns 1 when a state was read.
__declspec(dllexport) int ReadDeviceState(int slot, int* axes, unsigned char* buttons, int buttonCount)
{
    if (slot < 0 || slot >= g_readCount || !axes) return 0;
    ReadSlot& s = g_read[slot];
    LPDIRECTINPUTDEVICE8 dev = s.isWheel ? g_device : s.dev;
    if (!dev) return 0;
    DIJOYSTATE2 st = {};
    HRESULT hr = dev->Poll();
    if (FAILED(hr) && !s.isWheel) { dev->Acquire(); dev->Poll(); }
    hr = dev->GetDeviceState(sizeof(st), &st);
    if (FAILED(hr)) {
        // The wheel's acquisition is the force-feedback path's business.
        if (!s.isWheel && (hr == DIERR_INPUTLOST || hr == DIERR_NOTACQUIRED)) dev->Acquire();
        return 0;
    }
    axes[0] = st.lX;  axes[1] = st.lY;  axes[2] = st.lZ;
    axes[3] = st.lRx; axes[4] = st.lRy; axes[5] = st.lRz;
    axes[6] = st.rglSlider[0]; axes[7] = st.rglSlider[1];
    if (buttons && buttonCount > 0) {
        int n = buttonCount < 128 ? buttonCount : 128;
        for (int i = 0; i < n; ++i) buttons[i] = (st.rgbButtons[i] & 0x80) ? 1 : 0;
    }
    return 1;
}

__declspec(dllexport) void CloseReadDevices(void)
{
    for (int i = 0; i < g_readCount; ++i) {
        if (g_read[i].dev) { g_read[i].dev->Unacquire(); g_read[i].dev->Release(); g_read[i].dev = nullptr; }
    }
    if (g_readCount) Log("CloseReadDevices(): %d slot(s) released", g_readCount);
    g_readCount = 0;
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

    // Direction {|x|, |y|} - always the +X half-plane - and a SIGNED magnitude.
    // Three wheels, three readings of the same call, and this is the one
    // encoding all of them accept:
    //   - MOZA R5 honours the direction AND the magnitude sign, so signing
    //     both (the original code) double-negated - one side inverted;
    //   - MOZA R12 ignores the direction and reads the magnitude sign, so an
    //     unsigned magnitude left it with no sign (anti-centring);
    //   - single-axis wheels (Fanatec) ignore direction, signed magnitude.
    // The direction's LENGTH is kept equal to |magnitude|, not a constant
    // {1,0}: every build that used a constant unit direction saw the R12 start
    // rejecting updates with DIERR_INCOMPLETEEFFECT roughly 45 s after the
    // effect was created, and no build that kept the lengths equal ever did.
    // Direction is re-sent every update; some drivers drop it otherwise.
    LONG direction[2] = { x < 0 ? -(LONG)x : (LONG)x, y < 0 ? -(LONG)y : (LONG)y };
    DICONSTANTFORCE constant = {};
    constant.lMagnitude = (LONG)x;

    DIEFFECT update      = {};
    update.dwSize        = sizeof(DIEFFECT);
    update.dwFlags       = DIEFF_CARTESIAN | DIEFF_OBJECTOFFSETS;
    // MUST match the axis count the effect was created with. Hard-coding 2 here
    // while the effect fell back to 1 axis makes every update fail with
    // E_INVALIDARG, and the wheel simply never moves.
    update.cAxes         = g_effectAxes;
    update.rglDirection  = direction;
    update.cbTypeSpecificParams  = sizeof(DICONSTANTFORCE);
    update.lpvTypeSpecificParams = &constant;

    HRESULT hr = g_effect->SetParameters(
        &update, DIEP_DIRECTION | DIEP_TYPESPECIFICPARAMS | DIEP_START);

    // DIERR_NOTEXCLUSIVEACQUIRED (0x80040205): the wheel is acquired, but not
    // exclusively, and force feedback needs exclusive access. It happens
    // when the game loses the foreground (alt-tab) and the device comes back
    // non-exclusive; nothing then re-acquires it, so every update from that
    // moment is refused - 4,401 in one session on a MOZA R12, no force at
    // all. (Misread as DIERR_INCOMPLETEEFFECT and DIERR_EFFECTPLAYING for a
    // day; the SDK header says 0x80040205 is NOTEXCLUSIVEACQUIRED.)
    // Fix: drop the device and acquire it again, which is exclusive once the
    // game is in front, then retry the update. Rate-limited.
    if (hr == DIERR_NOTEXCLUSIVEACQUIRED || hr == DIERR_INPUTLOST || hr == DIERR_NOTACQUIRED) {
        static DWORD lastReacquire = 0;
        static long  reacquires = 0;
        DWORD now = GetTickCount();
        if (lastReacquire == 0 || now - lastReacquire > 250) {
            lastReacquire = now;
            HRESULT lost = hr;
            g_device->Unacquire();
            HRESULT ah = g_device->Acquire();
            if (SUCCEEDED(ah))
                hr = g_effect->SetParameters(
                    &update, DIEP_DIRECTION | DIEP_TYPESPECIFICPARAMS | DIEP_START);
            if (++reacquires <= 20 || reacquires % 100 == 0)
                Log("access lost (0x%08lX); re-acquire -> 0x%08lX, update -> 0x%08lX (#%ld)",
                    lost, ah, hr, reacquires);
        }
    }

    // Self-heal for anything else. A MOZA R12 started answering every update with
    // DIERR_INCOMPLETEEFFECT a few minutes into a session and never recovered
    // - 3,230 rejected updates in a minute, a dead wheel. The effect object
    // is unrecoverable at that point; a fresh one accepts parameters again.
    // Rate-limited so a genuinely broken device cannot spin this.
    if (FAILED(hr) && hr != DIERR_INPUTLOST && hr != DIERR_NOTACQUIRED && hr != DIERR_NOTEXCLUSIVEACQUIRED) {
        static DWORD lastHeal = 0;
        static int   healCount = 0;
        DWORD status = 0;
        if (SUCCEEDED(g_effect->GetEffectStatus(&status)))
            Log("  effect status 0x%08lX before heal", status);
        DWORD now = GetTickCount();
        if (lastHeal == 0 || now - lastHeal > 1000) {
            lastHeal = now;
            Log("SetParameters failed 0x%08lX - recreating the effect (heal #%d)", hr, ++healCount);
            g_device->Acquire();
            if (g_effect) { g_effect->Stop(); g_effect->Release(); g_effect = nullptr; }
            HRESULT ch = CreateForceEffect();
            if (SUCCEEDED(ch) && g_effect) {
                hr = g_effect->SetParameters(
                    &update, DIEP_DIRECTION | DIEP_TYPESPECIFICPARAMS | DIEP_START);
                Log("  recreated effect: update now 0x%08lX", hr);
            } else {
                Log("  recreate failed 0x%08lX", ch);
            }
        }
    }

    LeaveCriticalSection(&g_lock);

    if (hr == DIERR_INPUTLOST || hr == DIERR_NOTACQUIRED) {
        g_device->Acquire();
        return 0;
    }

    // Log failures, rate-limited. Without this a wheel that accepts the effect
    // and then rejects every update looks identical in the log to one that is
    // working perfectly - which is exactly how a real bug went unnoticed until a
    // user with different hardware reported it.
    if (FAILED(hr)) {
        static DWORD lastReport = 0;
        static long  failures   = 0;
        ++failures;
        DWORD now = GetTickCount();
        if (lastReport == 0 || now - lastReport > 5000) {
            lastReport = now;
            Log("SetParameters FAILED 0x%08lX (%ld failures so far; effect has %lu axis/axes) "
                "- no force is reaching the wheel", hr, failures, g_effectAxes);
        }
        return 0;
    }
    return 1;
}

__declspec(dllexport) BOOL StartEffect(void)
{
    if (!g_effect) { Log("StartEffect() with no effect"); return FALSE; }
    HRESULT hr = g_effect->Start(1, 0);
    Log("StartEffect() -> 0x%08lX", hr);
    return SUCCEEDED(hr) ? TRUE : FALSE;
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

    HRESULT hr = g_device->SetProperty(DIPROP_AUTOCENTER, &prop.diph);
    if (FAILED(hr)) Log("  SetProperty(AUTOCENTER) -> 0x%08lX", hr);
    return SUCCEEDED(hr) ? TRUE : FALSE;
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
