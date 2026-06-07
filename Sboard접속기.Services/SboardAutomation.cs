using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Sboard접속기.Services;

public class SboardAutomation
{
    private const string LoginWindowTitle = "Sboard";
    private const string SessionPrefix = "Sboard [";
    private const string EditClassName = "Edit";

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
        Thread.Sleep(200);

        var fields = FindEditFields(hwnd);
        if (fields.Count >= 2)
        {
            var (idField, pwField) = (fields[0], fields[1]);
            TypeTextInField(idField, hwnd, id);
            TypeTextInField(pwField, hwnd, pw);
        }
        else
        {
            FallbackInput(hwnd, id, pw);
        }

        Thread.Sleep(200);
        ClickLoginButton(hwnd);
    }

    private List<IntPtr> FindEditFields(IntPtr parentHwnd)
    {
        var fields = new List<IntPtr>();
        var classNameBuf = new StringBuilder(256);

        NativeMethods.EnumChildWindows(parentHwnd, (child, _) =>
        {
            if (!NativeMethods.IsWindowVisible(child)) return true;
            classNameBuf.Clear();
            NativeMethods.GetClassNameW(child, classNameBuf, classNameBuf.Capacity);
            if (classNameBuf.ToString() == EditClassName)
                fields.Add(child);
            return true;
        }, IntPtr.Zero);

        return fields;
    }

    private void TypeTextInField(IntPtr fieldHwnd, IntPtr loginHwnd, string text)
    {
        NativeMethods.SendMessageW(fieldHwnd, NativeMethods.WM_SETTEXT, IntPtr.Zero, new StringBuilder(text));
    }

    private void ClickLoginButton(IntPtr loginHwnd)
    {
        var buttons = new List<IntPtr>();
        var classNameBuf = new StringBuilder(256);

        NativeMethods.EnumChildWindows(loginHwnd, (child, _) =>
        {
            if (!NativeMethods.IsWindowVisible(child)) return true;
            classNameBuf.Clear();
            NativeMethods.GetClassNameW(child, classNameBuf, classNameBuf.Capacity);
            if (classNameBuf.ToString() == NativeMethods.ButtonClassName)
                buttons.Add(child);
            return true;
        }, IntPtr.Zero);

        var btn = buttons.FirstOrDefault();
        if (btn != IntPtr.Zero)
        {
            NativeMethods.SendMessageW(btn, NativeMethods.BM_CLICK, IntPtr.Zero, IntPtr.Zero);
        }
        else
        {
            NativeMethods.SendKeyPress((ushort)NativeMethods.VK_RETURN);
        }
    }

    private void FallbackInput(IntPtr hwnd, string id, string pw)
    {
        NativeMethods.SetForegroundWindow(hwnd);
        Thread.Sleep(150);
        PressKey(NativeMethods.VK_TAB); Thread.Sleep(50);
        PressKey(NativeMethods.VK_TAB); Thread.Sleep(50);

        NativeMethods.SetForegroundWindow(hwnd);
        Thread.Sleep(150);
        SendKeysDirect(id);

        PressKey(NativeMethods.VK_TAB); Thread.Sleep(50);

        NativeMethods.SetForegroundWindow(hwnd);
        Thread.Sleep(150);
        SendKeysDirect(pw);
    }

    private static void SendKeysDirect(string text)
    {
        NativeMethods.SendUnicodeText(text);
    }

    public bool VerifyInput(IntPtr loginHwnd, string expectedId, string expectedPw)
    {
        var fields = FindEditFields(loginHwnd);
        if (fields.Count < 2) return false;

        bool idOk = GetFieldText(fields[0]) == expectedId;
        bool pwOk = GetFieldText(fields[1]) == expectedPw;
        return idOk && pwOk;
    }

    private static string GetFieldText(IntPtr fieldHwnd)
    {
        int len = (int)NativeMethods.SendMessageW(fieldHwnd, NativeMethods.WM_GETTEXTLENGTH, IntPtr.Zero, IntPtr.Zero);
        if (len <= 0) return "";
        var sb = new StringBuilder(len + 1);
        NativeMethods.SendMessageW(fieldHwnd, NativeMethods.WM_GETTEXT, (IntPtr)(len + 1), sb);
        return sb.ToString();
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
}
