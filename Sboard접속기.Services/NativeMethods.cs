using System.Runtime.InteropServices;
using System.Text;

namespace Sboard접속기.Services;

public static class NativeMethods
{
    public const int KEYEVENTF_KEYUP = 0x0002;
    public const int VK_TAB = 0x09;
    public const int VK_RETURN = 0x0D;
    public const int VK_CONTROL = 0x11;
    public const int VK_V = 0x56;
    public const int SW_SHOW = 5;
    public const uint INFINITE = 0xFFFFFFFF;
    public const int ERROR_ALREADY_EXISTS = 183;

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern IntPtr FindWindowW(string? lpClassName, string lpWindowName);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, IntPtr dwExtraInfo);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern int GetWindowTextLengthW(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern int GetWindowTextW(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

    public const uint GW_HWNDNEXT = 2;

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    public static extern IntPtr ShellExecuteW(IntPtr hwnd, string lpOperation, string lpFile, string? lpParameters, string? lpDirectory, int nShowCmd);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern IntPtr CreateMutexW(IntPtr lpMutexAttributes, [MarshalAs(UnmanagedType.Bool)] bool bInitialOwner, string lpName);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool CloseHandle(IntPtr hObject);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern uint GetLastError();

    [DllImport("shell32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsUserAnAdmin();

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool EnumChildWindows(IntPtr hWndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern int GetClassNameW(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern IntPtr SendMessageW(IntPtr hWnd, uint Msg, IntPtr wParam, StringBuilder lParam);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr SendMessageW(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    public const uint WM_GETTEXT = 0x000D;
    public const uint WM_GETTEXTLENGTH = 0x000E;
    public const uint EM_GETPASSWORDCHAR = 0x00D2;
    public const uint WM_SETTEXT = 0x000C;
    public const uint BM_CLICK = 0x00F5;
    public const string ButtonClassName = "Button";

    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    public const uint INPUT_KEYBOARD = 1;
    public const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
    public const uint KEYEVENTF_KEYUP_SEND = 0x0002;
    public const uint KEYEVENTF_SCANCODE = 0x0008;
    public const uint KEYEVENTF_UNICODE = 0x0004;

    [StructLayout(LayoutKind.Sequential)]
    public struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct MOUSEKEYBDHARDWAREINPUT
    {
        [FieldOffset(0)] public int mi;
        [FieldOffset(0)] public KEYBDINPUT ki;
        [FieldOffset(0)] public int hi;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct INPUT
    {
        public uint type;
        public MOUSEKEYBDHARDWAREINPUT u;
    }

    [DllImport("user32.dll", SetLastError = true)]
    public static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr GetMessageExtraInfo();

    public static void SendKeyPress(ushort vk)
    {
        var inputs = new INPUT[2];
        var kiDown = new KEYBDINPUT { wVk = vk, dwExtraInfo = GetMessageExtraInfo() };
        var kiUp = new KEYBDINPUT { wVk = vk, dwFlags = KEYEVENTF_KEYUP_SEND, dwExtraInfo = GetMessageExtraInfo() };
        inputs[0] = new INPUT { type = INPUT_KEYBOARD, u = new MOUSEKEYBDHARDWAREINPUT { ki = kiDown } };
        inputs[1] = new INPUT { type = INPUT_KEYBOARD, u = new MOUSEKEYBDHARDWAREINPUT { ki = kiUp } };
        SendInput(2, inputs, Marshal.SizeOf<INPUT>());
    }

    public static void SendUnicodeText(string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        var inputs = new INPUT[text.Length * 2];
        int idx = 0;
        var extra = GetMessageExtraInfo();
        foreach (char c in text)
        {
            inputs[idx] = new INPUT
            {
                type = INPUT_KEYBOARD,
                u = new MOUSEKEYBDHARDWAREINPUT
                {
                    ki = new KEYBDINPUT { wVk = 0, wScan = c, dwFlags = KEYEVENTF_UNICODE, dwExtraInfo = extra }
                }
            };
            idx++;
            inputs[idx] = new INPUT
            {
                type = INPUT_KEYBOARD,
                u = new MOUSEKEYBDHARDWAREINPUT
                {
                    ki = new KEYBDINPUT { wVk = 0, wScan = c, dwFlags = KEYEVENTF_UNICODE | KEYEVENTF_KEYUP_SEND, dwExtraInfo = extra }
                }
            };
            idx++;
        }
        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }
}
