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

    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
}
