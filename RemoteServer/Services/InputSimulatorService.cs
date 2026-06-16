using System.Runtime.InteropServices;

namespace RemoteServer.Services;

public class InputSimulatorService
{
    [DllImport("user32.dll")]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll")]
    private static extern IntPtr GetMessageExtraInfo();

    private const int INPUT_MOUSE = 0;
    private const int INPUT_KEYBOARD = 1;

    private const int MOUSEEVENTF_MOVE = 0x0001;
    private const int MOUSEEVENTF_ABSOLUTE = 0x8000;
    private const int MOUSEEVENTF_LEFTDOWN = 0x0002;
    private const int MOUSEEVENTF_LEFTUP = 0x0004;
    private const int MOUSEEVENTF_RIGHTDOWN = 0x0008;
    private const int MOUSEEVENTF_RIGHTUP = 0x0010;
    private const int MOUSEEVENTF_MIDDLEDOWN = 0x0020;
    private const int MOUSEEVENTF_MIDDLEUP = 0x0040;
    private const int MOUSEEVENTF_WHEEL = 0x0800;

    private const int KEYEVENTF_KEYDOWN = 0x0000;
    private const int KEYEVENTF_KEYUP = 0x0002;

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public int type;
        public INPUT_UNION u;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct INPUT_UNION
    {
        [FieldOffset(0)] public MOUSEINPUT mi;
        [FieldOffset(0)] public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public int mouseData;
        public int dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public short wVk;
        public short wScan;
        public int dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    private int _screenWidth;
    private int _screenHeight;

    public InputSimulatorService()
    {
        _screenWidth = (int)System.Windows.SystemParameters.PrimaryScreenWidth;
        _screenHeight = (int)System.Windows.SystemParameters.PrimaryScreenHeight;
    }

    public void MouseMove(int x, int y)
    {
        var input = new INPUT
        {
            type = INPUT_MOUSE,
            u = new INPUT_UNION
            {
                mi = new MOUSEINPUT
                {
                    dx = (x * 65535) / _screenWidth,
                    dy = (y * 65535) / _screenHeight,
                    dwFlags = MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE,
                    dwExtraInfo = GetMessageExtraInfo()
                }
            }
        };
        SendInput(1, [input], Marshal.SizeOf<INPUT>());
    }

    public void MouseDown(int button)
    {
        int flag = button switch { 0 => MOUSEEVENTF_LEFTDOWN, 1 => MOUSEEVENTF_RIGHTDOWN, _ => MOUSEEVENTF_MIDDLEDOWN };
        SendMouseFlag(flag);
    }

    public void MouseUp(int button)
    {
        int flag = button switch { 0 => MOUSEEVENTF_LEFTUP, 1 => MOUSEEVENTF_RIGHTUP, _ => MOUSEEVENTF_MIDDLEUP };
        SendMouseFlag(flag);
    }

    public void MouseWheel(int delta)
    {
        var input = new INPUT
        {
            type = INPUT_MOUSE,
            u = new INPUT_UNION
            {
                mi = new MOUSEINPUT { mouseData = delta, dwFlags = MOUSEEVENTF_WHEEL, dwExtraInfo = GetMessageExtraInfo() }
            }
        };
        SendInput(1, [input], Marshal.SizeOf<INPUT>());
    }

    public void KeyDown(int keyCode)
    {
        SendKey((short)keyCode, KEYEVENTF_KEYDOWN);
    }

    public void KeyUp(int keyCode)
    {
        SendKey((short)keyCode, KEYEVENTF_KEYUP);
    }

    private void SendMouseFlag(int flag)
    {
        var input = new INPUT
        {
            type = INPUT_MOUSE,
            u = new INPUT_UNION
            {
                mi = new MOUSEINPUT { dwFlags = flag, dwExtraInfo = GetMessageExtraInfo() }
            }
        };
        SendInput(1, [input], Marshal.SizeOf<INPUT>());
    }

    private static void SendKey(short keyCode, int flags)
    {
        var input = new INPUT
        {
            type = INPUT_KEYBOARD,
            u = new INPUT_UNION
            {
                ki = new KEYBDINPUT { wVk = keyCode, dwFlags = flags, dwExtraInfo = GetMessageExtraInfo() }
            }
        };
        SendInput(1, [input], Marshal.SizeOf<INPUT>());
    }
}
