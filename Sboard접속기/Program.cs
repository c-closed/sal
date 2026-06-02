using Sboard접속기.Forms;
using Sboard접속기.Services;

namespace Sboard접속기;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        if (!AdminHelper.IsAdministrator())
        {
            if (AdminHelper.TryRestartAsAdmin(""))
                return;
            MessageBox.Show("관리자 권한으로 실행해야 SendInput이 동작합니다.\n관리자 권한으로 다시 실행해주세요.",
                "Sboard 접속기", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using var single = new SingleInstance();
        if (!single.TryAcquire(Config.MutexName))
        {
            MessageBox.Show("Sboard 접속기가 이미 실행 중입니다.", "Sboard 접속기", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var api = new ApiService(Config.ApiBaseUrl, Config.BearerToken);
        var auto = new SboardAutomation();

        Application.Run(new MainForm(api, auto));
    }
}
