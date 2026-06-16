using System.Diagnostics;
using System.Runtime.InteropServices;
using SharedLib;

namespace RemoteClient.Services;

public class InputCaptureService : IDisposable
{
    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
    private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll")]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll")]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    private const int WH_KEYBOARD_LL = 13;
    private const int WH_MOUSE_LL = 14;

    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int WM_SYSKEYUP = 0x0105;
    private const int WM_MOUSEMOVE = 0x0200;
    private const int WM_LBUTTONDOWN = 0x0201;
    private const int WM_LBUTTONUP = 0x0202;
    private const int WM_RBUTTONDOWN = 0x0204;
    private const int WM_RBUTTONUP = 0x0205;
    private const int WM_MBUTTONDOWN = 0x0207;
    private const int WM_MBUTTONUP = 0x0208;
    private const int WM_MOUSEWHEEL = 0x020A;

    private LowLevelKeyboardProc? _keyboardProc;
    private LowLevelMouseProc? _mouseProc;
    private IntPtr _keyboardHookId = IntPtr.Zero;
    private IntPtr _mouseHookId = IntPtr.Zero;

    private string? _sessionCode;
    private SignalRClient? _signalR;
    private bool _capturing;

    public void StartCapture(SignalRClient signalR, string sessionCode)
    {
        _signalR = signalR;
        _sessionCode = sessionCode;
        _capturing = true;

        _keyboardProc = KeyboardHookCallback;
        _mouseProc = MouseHookCallback;

        using var curProcess = Process.GetCurrentProcess();
        using var mainModule = curProcess.MainModule!;
        var moduleHandle = GetModuleHandle(mainModule.ModuleName);

        _keyboardHookId = SetWindowsHookEx(WH_KEYBOARD_LL, _keyboardProc, moduleHandle, 0);
        _mouseHookId = SetWindowsHookEx(WH_MOUSE_LL, _mouseProc, moduleHandle, 0);
    }

    public void StopCapture()
    {
        _capturing = false;
        if (_keyboardHookId != IntPtr.Zero)
            UnhookWindowsHookEx(_keyboardHookId);
        if (_mouseHookId != IntPtr.Zero)
            UnhookWindowsHookEx(_mouseHookId);
    }

    private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && _capturing && _signalR != null && _sessionCode != null)
        {
            int vkCode = Marshal.ReadInt32(lParam);
            bool isDown = wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN;
            bool isUp = wParam == (IntPtr)WM_KEYUP || wParam == (IntPtr)WM_SYSKEYUP;

            if (isDown || isUp)
            {
                var input = new InputData
                {
                    Type = isDown ? "KeyDown" : "KeyUp",
                    KeyCode = vkCode
                };
                _ = _signalR.SendInput(_sessionCode, input);
                return (IntPtr)1;
            }
        }
        return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
    }

    private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && _capturing && _signalR != null && _sessionCode != null)
        {
            var hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);

            switch ((int)wParam)
            {
                case WM_MOUSEMOVE:
                    var input = new InputData { Type = "MouseMove", X = hookStruct.pt.x, Y = hookStruct.pt.y };
                    _ = _signalR.SendInput(_sessionCode, input);
                    break;
                case WM_LBUTTONDOWN:
                    _ = _signalR.SendInput(_sessionCode, new InputData { Type = "MouseDown", Button = 0 });
                    break;
                case WM_LBUTTONUP:
                    _ = _signalR.SendInput(_sessionCode, new InputData { Type = "MouseUp", Button = 0 });
                    break;
                case WM_RBUTTONDOWN:
                    _ = _signalR.SendInput(_sessionCode, new InputData { Type = "MouseDown", Button = 1 });
                    break;
                case WM_RBUTTONUP:
                    _ = _signalR.SendInput(_sessionCode, new InputData { Type = "MouseUp", Button = 1 });
                    break;
                case WM_MOUSEWHEEL:
                    int delta = (short)((hookStruct.mouseData >> 16) & 0xFFFF);
                    _ = _signalR.SendInput(_sessionCode, new InputData { Type = "MouseWheel", Delta = delta });
                    break;
            }
            return (IntPtr)1;
        }
        return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int x; public int y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MSLLHOOKSTRUCT
    {
        public POINT pt;
        public int mouseData;
        public int flags;
        public int time;
        public IntPtr dwExtraInfo;
    }

    public void Dispose()
    {
        StopCapture();
    }
}
