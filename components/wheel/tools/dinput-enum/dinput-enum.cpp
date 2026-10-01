// dinput-enum — list every DirectInput game controller and its force feedback
// capability, using the same API art of rally's Rewired backend and our
// UnityForceFeedback.dll both use.
//
// This answers a question nothing else on Windows answers cleanly: not "is the
// device plugged in" (Device Manager shows that) but "can DirectInput see it,
// and can it take force feedback effects". A wheel that Windows enumerates as a
// vendor-defined HID device is invisible here, and therefore invisible to the
// game — no mod and no binding utility can conjure it into existence.
//
// Build:  cl /nologo /O2 /EHsc dinput-enum.cpp /link dinput8.lib dxguid.lib ole32.lib user32.lib

#define DIRECTINPUT_VERSION 0x0800
#define WIN32_LEAN_AND_MEAN

#include <windows.h>
#include <dinput.h>
#include <cstdio>

#pragma comment(lib, "dinput8.lib")
#pragma comment(lib, "dxguid.lib")
#pragma comment(lib, "ole32.lib")
#pragma comment(lib, "user32.lib")

static LPDIRECTINPUT8 g_di = nullptr;
static int g_count = 0;
static int g_ffbCount = 0;

static const char* DeviceTypeName(DWORD type)
{
    switch (GET_DIDEVICE_TYPE(type))
    {
        case DI8DEVTYPE_DRIVING:       return "DRIVING (wheel)";
        case DI8DEVTYPE_GAMEPAD:       return "GAMEPAD";
        case DI8DEVTYPE_JOYSTICK:      return "JOYSTICK";
        case DI8DEVTYPE_FLIGHT:        return "FLIGHT";
        case DI8DEVTYPE_1STPERSON:     return "1STPERSON";
        case DI8DEVTYPE_SUPPLEMENTAL:  return "SUPPLEMENTAL (pedals/shifter)";
        case DI8DEVTYPE_DEVICECTRL:    return "DEVICECTRL";
        case DI8DEVTYPE_KEYBOARD:      return "KEYBOARD";
        case DI8DEVTYPE_MOUSE:         return "MOUSE";
        default:                       return "OTHER";
    }
}

static BOOL CALLBACK EnumAxesCallback(const DIDEVICEOBJECTINSTANCE* obj, VOID* ctx)
{
    int* axes = (int*)ctx;
    (*axes)++;
    printf("        axis: %s%s\n", obj->tszName,
           (obj->dwFlags & DIDOI_FFACTUATOR) ? "   [FFB actuator]" : "");
    return DIENUM_CONTINUE;
}

static BOOL CALLBACK EnumCallback(const DIDEVICEINSTANCE* inst, VOID* ctx)
{
    (void)ctx;
    g_count++;

    printf("\n[%d] %s\n", g_count, inst->tszProductName);
    printf("    instance name : %s\n", inst->tszInstanceName);
    printf("    type          : %s\n", DeviceTypeName(inst->dwDevType));

    // The GUID's first 4 bytes are VID/PID for HID devices - the same identity
    // Rewired matches against its hardware map database.
    const unsigned char* g = (const unsigned char*)&inst->guidProduct;
    printf("    VID/PID       : VID_%02X%02X&PID_%02X%02X\n", g[1], g[0], g[3], g[2]);

    LPDIRECTINPUTDEVICE8 dev = nullptr;
    if (FAILED(g_di->CreateDevice(inst->guidInstance, &dev, nullptr)))
    {
        printf("    !! could not open device\n");
        return DIENUM_CONTINUE;
    }

    DIDEVCAPS caps = {};
    caps.dwSize = sizeof(caps);
    if (SUCCEEDED(dev->GetCapabilities(&caps)))
    {
        bool ffb = (caps.dwFlags & DIDC_FORCEFEEDBACK) != 0;
        if (ffb) g_ffbCount++;
        printf("    axes/buttons  : %u axes, %u buttons, %u POV\n",
               caps.dwAxes, caps.dwButtons, caps.dwPOVs);
        printf("    FORCE FEEDBACK: %s\n", ffb ? "YES" : "no");

        int axes = 0;
        dev->EnumObjects(EnumAxesCallback, &axes, DIDFT_AXIS);
    }

    dev->Release();
    return DIENUM_CONTINUE;
}

int main()
{
    printf("DirectInput 8 game controller enumeration\n");
    printf("=========================================\n");
    printf("This is exactly what art of rally (via Rewired) and\n");
    printf("UnityForceFeedback.dll can see. A device absent here is\n");
    printf("invisible to the game no matter what mod is installed.\n");

    HRESULT hr = DirectInput8Create(GetModuleHandle(nullptr), DIRECTINPUT_VERSION,
                                    IID_IDirectInput8, (VOID**)&g_di, nullptr);
    if (FAILED(hr))
    {
        printf("\nDirectInput8Create failed: 0x%08lX\n", hr);
        return 1;
    }

    // DI8DEVCLASS_GAMECTRL covers wheels, pedals, shifters, gamepads, sticks.
    g_di->EnumDevices(DI8DEVCLASS_GAMECTRL, EnumCallback, nullptr, DIEDFL_ATTACHEDONLY);

    printf("\n=========================================\n");
    printf("%d game controller(s) visible to DirectInput\n", g_count);
    printf("%d with force feedback\n", g_ffbCount);
    if (g_count == 0)
        printf("\nNothing found. Check the device is powered on and not in a\n"
               "vendor-only / XInput-only mode.\n");

    g_di->Release();
    return 0;
}
