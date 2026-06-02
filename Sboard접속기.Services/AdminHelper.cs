using System.Diagnostics;

namespace Sboard접속기.Services;

public static class AdminHelper
{
    public static bool IsAdministrator()
    {
        return NativeMethods.IsUserAnAdmin();
    }

    public static bool TryRestartAsAdmin(string args)
    {
        var path = Environment.ProcessPath;
        if (path is null) return false;

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = path,
                Arguments = args,
                UseShellExecute = true,
                Verb = "runas"
            };
            using var proc = Process.Start(psi);
            if (proc is not null) return true;
        }
        catch
        {
            // ShellExecuteW fallback for Korean path encoding
        }

        try
        {
            var ret = NativeMethods.ShellExecuteW(IntPtr.Zero, "runas", path, args, null, 1);
            if (ret.ToInt64() > 32) return true;
        }
        catch
        {
            // both methods failed
        }
        return false;
    }
}
