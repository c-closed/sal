using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Sboard접속기.Services;

public class SboardAutomation
{
    private const string LoginWindowTitle = "Sboard";
    private const string SessionPrefix = "Sboard [";

    public bool LaunchSboard()
    {
        var paths = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "sboard.exe"),
            Path.Combine(AppContext.BaseDirectory, "Sboard.exe"),
            @"C:\Program Files (x86)\sprog\sboard.exe",
            @"C:\Program Files\sprog\sboard.exe"
        };

        string? envPath = Environment.GetEnvironmentVariable("SBOARD_EXE_PATH");
        if (!string.IsNullOrEmpty(envPath) && File.Exists(envPath))
            paths = [envPath];

        foreach (var p in paths)
        {
            if (!File.Exists(p)) continue;
            try
            {
                var psi = new ProcessStartInfo(p)
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                Process.Start(psi);
                return true;
            }
            catch (Exception ex) when (ex is Win32Exception we && we.NativeErrorCode == 740)
            {
                NativeMethods.ShellExecuteW(IntPtr.Zero, "runas", p, null, null, NativeMethods.SW_SHOW);
                return true;
            }
            catch { continue; }
        }
        return false;
    }

    public IntPtr FindLoginWindow()
    {
        IntPtr found = IntPtr.Zero;
        NativeMethods.EnumWindows((hwnd, _) =>
        {
            int len = NativeMethods.GetWindowTextLengthW(hwnd);
            if (len > 0)
            {
                var sb = new StringBuilder(len + 1);
                NativeMethods.GetWindowTextW(hwnd, sb, sb.Capacity);
                if (sb.ToString().Trim() == LoginWindowTitle)
                    found = hwnd;
            }
            return found == IntPtr.Zero;
        }, IntPtr.Zero);
        return found;
    }

    public IntPtr FindSessionWindow()
    {
        IntPtr found = IntPtr.Zero;
        NativeMethods.EnumWindows((hwnd, _) =>
        {
            int len = NativeMethods.GetWindowTextLengthW(hwnd);
            if (len > 0)
            {
                var sb = new StringBuilder(len + 1);
                NativeMethods.GetWindowTextW(hwnd, sb, sb.Capacity);
                if (sb.ToString().StartsWith(SessionPrefix))
                    found = hwnd;
            }
            return found == IntPtr.Zero;
        }, IntPtr.Zero);
        return found;
    }

    public List<(IntPtr Hwnd, string Title)> FindAllWindowsByTitle(string titlePart)
    {
        var results = new List<(IntPtr, string)>();
        NativeMethods.EnumWindows((hwnd, _) =>
        {
            if (!NativeMethods.IsWindowVisible(hwnd)) return true;
            int len = NativeMethods.GetWindowTextLengthW(hwnd);
            if (len > 0)
            {
                var sb = new StringBuilder(len + 1);
                NativeMethods.GetWindowTextW(hwnd, sb, sb.Capacity);
                var title = sb.ToString();
                if (title.Contains(titlePart, StringComparison.OrdinalIgnoreCase))
                    results.Add((hwnd, title));
            }
            return true;
        }, IntPtr.Zero);
        return results;
    }

    public void InputCredentials(IntPtr hwnd, string id, string pw)
    {
        NativeMethods.SetForegroundWindow(hwnd);
        Thread.Sleep(150);

        PressKey(NativeMethods.VK_TAB); Thread.Sleep(50);
        PressKey(NativeMethods.VK_TAB); Thread.Sleep(50);

        NativeMethods.SetForegroundWindow(hwnd);
        Thread.Sleep(150);
        SetClipboard(id);
        PasteWithControlV();

        PressKey(NativeMethods.VK_TAB); Thread.Sleep(50);

        NativeMethods.SetForegroundWindow(hwnd);
        Thread.Sleep(150);
        SetClipboard(pw);
        PasteWithControlV();

        Thread.Sleep(100);
        PressKey(NativeMethods.VK_RETURN);
    }

    public void KillSboard()
    {
        try
        {
            foreach (var proc in Process.GetProcessesByName("sboard"))
                proc.Kill();
        }
        catch { }
    }

    public uint GetProcessId(IntPtr hwnd)
    {
        NativeMethods.GetWindowThreadProcessId(hwnd, out uint pid);
        return pid;
    }

    public IntPtr DetectErrorDialog(uint pid)
    {
        IntPtr found = IntPtr.Zero;
        NativeMethods.EnumWindows((hwnd, _) =>
        {
            NativeMethods.GetWindowThreadProcessId(hwnd, out uint wpid);
            if (wpid != pid) return true;
            if (!NativeMethods.IsWindowVisible(hwnd)) return true;
            int len = NativeMethods.GetWindowTextLengthW(hwnd);
            if (len > 0)
            {
                var sb = new StringBuilder(len + 1);
                NativeMethods.GetWindowTextW(hwnd, sb, sb.Capacity);
                var title = sb.ToString();
                if (title.Contains("Sboard") && !title.StartsWith(SessionPrefix))
                    found = hwnd;
            }
            return found == IntPtr.Zero;
        }, IntPtr.Zero);
        return found;
    }

    public string GetWindowText(IntPtr hwnd)
    {
        int len = NativeMethods.GetWindowTextLengthW(hwnd);
        if (len <= 0) return "";
        var sb = new StringBuilder(len + 1);
        NativeMethods.GetWindowTextW(hwnd, sb, sb.Capacity);
        return sb.ToString();
    }

    private static void PressKey(byte vk)
    {
        NativeMethods.keybd_event(vk, 0, 0, IntPtr.Zero);
        Thread.Sleep(20);
        NativeMethods.keybd_event(vk, 0, NativeMethods.KEYEVENTF_KEYUP, IntPtr.Zero);
        Thread.Sleep(20);
    }

    private static void SetClipboard(string text)
    {
        Thread staThread = new(() => Clipboard.SetText(text));
        staThread.SetApartmentState(ApartmentState.STA);
        staThread.Start();
        staThread.Join();
    }

    private static void PasteWithControlV()
    {
        NativeMethods.keybd_event(NativeMethods.VK_CONTROL, 0, 0, IntPtr.Zero);
        Thread.Sleep(10);
        NativeMethods.keybd_event(NativeMethods.VK_V, 0, 0, IntPtr.Zero);
        Thread.Sleep(10);
        NativeMethods.keybd_event(NativeMethods.VK_V, 0, NativeMethods.KEYEVENTF_KEYUP, IntPtr.Zero);
        Thread.Sleep(10);
        NativeMethods.keybd_event(NativeMethods.VK_CONTROL, 0, NativeMethods.KEYEVENTF_KEYUP, IntPtr.Zero);
        Thread.Sleep(10);
    }
}
