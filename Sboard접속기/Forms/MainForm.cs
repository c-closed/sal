using System.Data;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using AutoUpdaterDotNET;
using Sboard접속기.Services;
using Sboard접속기.Services.Models;

namespace Sboard접속기.Forms;

public partial class MainForm : Form
{
    private readonly ApiService _api;
    private readonly SboardAutomation _auto;
    private Dictionary<string, UserInfo> _users = [];
    private bool _busy;

    public MainForm(ApiService api, SboardAutomation auto)
    {
        _api = api;
        _auto = auto;
        InitializeComponent();
        LoadIcon();
        WireEvents();
        Load += MainForm_Load;
    }

    private void WireEvents()
    {
        btnLogin.Click += BtnLogin_Click;
        btnManage.Click += BtnManage_Click;
        txtUsername.KeyPress += TxtUsername_KeyPress;
        dgvUsers.CellDoubleClick += DgvUsers_CellDoubleClick;
    }

    private void LoadIcon()
    {
        var paths = new[]
        {
            Path.Combine(Application.StartupPath, "icon.ico"),
            Path.Combine(Application.StartupPath, "_internal", "icon.ico")
        };
        foreach (var p in paths)
        {
            if (File.Exists(p))
            {
                Icon = new Icon(p);
                return;
    }
}

    }

    [DllImport("dwmapi")]
    static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    private async void MainForm_Load(object? sender, EventArgs e)
    {
        ApplyModernStyle();
        lblStatus.BringToFront();

        AutoUpdater.Mandatory = true;
        AutoUpdater.RunUpdateAsAdmin = true;
        AutoUpdater.ReportErrors = false;
        AutoUpdater.Start(Config.UpdateXmlUrl);

        await LoadUsersAsync();
    }

    private void ApplyModernStyle()
    {
        dgvUsers.EnableHeadersVisualStyles = false;
        dgvUsers.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(45, 62, 80);
        dgvUsers.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        dgvUsers.ColumnHeadersDefaultCellStyle.Font = new Font("Consolas", 10F, FontStyle.Bold);
        dgvUsers.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        dgvUsers.ColumnHeadersHeight = 32;
        dgvUsers.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(245, 247, 250);
        dgvUsers.RowsDefaultCellStyle.BackColor = Color.White;
        dgvUsers.RowsDefaultCellStyle.ForeColor = Color.FromArgb(52, 73, 94);
        dgvUsers.RowsDefaultCellStyle.Font = new Font("Consolas", 10F);
        dgvUsers.RowsDefaultCellStyle.SelectionBackColor = Color.FromArgb(52, 152, 219);
        dgvUsers.RowsDefaultCellStyle.SelectionForeColor = Color.White;
        dgvUsers.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        dgvUsers.GridColor = Color.FromArgb(230, 235, 240);
        dgvUsers.BackgroundColor = Color.White;

        colName.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
        colId.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;

        void StyleButton(Button btn, Color bg, Color hover)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.BackColor = bg;
            btn.ForeColor = Color.White;
            btn.Font = new Font("Consolas", 10F, FontStyle.Bold);
            btn.Cursor = Cursors.Hand;
            btn.MouseEnter += (_, _) => btn.BackColor = hover;
            btn.MouseLeave += (_, _) => btn.BackColor = bg;
        }

        StyleButton(btnLogin, Color.FromArgb(52, 152, 219), Color.FromArgb(41, 128, 185));
        StyleButton(btnManage, Color.FromArgb(52, 152, 219), Color.FromArgb(41, 128, 185));

        gbLogin.ForeColor = Color.FromArgb(41, 128, 185);
        gbUsers.ForeColor = Color.FromArgb(41, 128, 185);
        BackColor = Color.White;
        txtUsername.Font = new Font("Consolas", 10F);
        txtUsername.BackColor = Color.White;
        txtUsername.BorderStyle = BorderStyle.FixedSingle;

        if (Environment.OSVersion.Version.Build >= 22000)
        {
            int pref = 2;
            DwmSetWindowAttribute(Handle, 33, ref pref, sizeof(int));
        }
    }

    private async Task LoadUsersAsync()
    {
        try
        {
            lblStatus.Text = "사용자 목록을 불러오는 중...";
            lblStatus.Visible = true;
            _users = await _api.GetUsersAsync();
            RefreshUserGrid();
            lblStatus.Visible = false;
        }
        catch
        {
            lblStatus.Text = "사용자 목록 불러오기 실패";
        }
    }

    private void dgvUsers_CellContentClick(object? sender, DataGridViewCellEventArgs e)
    {
    }

    private void RefreshUserGrid()
    {
        var dt = new DataTable();
        dt.Columns.Add("Name", typeof(string));
        dt.Columns.Add("Id", typeof(string));
        foreach (var kv in _users.OrderBy(x => x.Key))
            dt.Rows.Add(kv.Key, kv.Value.Id);
        dgvUsers.DataSource = dt;
    }

    private void BtnLogin_Click(object? sender, EventArgs e)
    {
        var username = txtUsername.Text.Trim();
        if (!Regex.IsMatch(username, @"^[가-힣]+$"))
        {
            MessageBox.Show("사용자명은 한글로만 입력해주세요.", "입력 오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (_busy) return;
        _ = DoLoginAsync(username);
    }

    private void DgvUsers_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || dgvUsers.Rows[e.RowIndex].Cells[0].Value is not string name) return;
        txtUsername.Text = name;
        BtnLogin_Click(null, EventArgs.Empty);
    }

    private void TxtUsername_KeyPress(object? sender, KeyPressEventArgs e)
    {
        if (e.KeyChar == (char)Keys.Enter)
        {
            e.Handled = true;
            BtnLogin_Click(null, EventArgs.Empty);
            return;
        }
        if (!char.IsControl(e.KeyChar) && !Regex.IsMatch(e.KeyChar.ToString(), @"[가-힣]"))
            e.Handled = true;
    }

    private async Task DoLoginAsync(string username)
    {
        _busy = true;
        btnLogin.Enabled = false;
        btnManage.Enabled = false;
        txtUsername.Enabled = false;

        var logForm = new LoginLogForm(username);
        logForm.Show(this);

        try
        {
            logForm.AppendLog("서버 연결 중...");
            if (!_users.TryGetValue(username, out var info))
            {
                _users = await _api.GetUsersAsync();
                RefreshUserGrid();
                if (!_users.TryGetValue(username, out info))
                {
                    logForm.AppendLog($"'{username}' 은 등록되지 않았습니다.");
                    return;
                }
            }

            logForm.AppendLog($"사용자 확인됨 (ID: {info.Id})");
            await RunSboardLoginAsync(logForm, username, info.Id, info.Pw);
        }
        catch (Exception ex)
        {
            logForm.AppendLog($"예외 발생: {ex.Message}");
        }
        finally
        {
            _busy = false;
            btnLogin.Enabled = true;
            btnManage.Enabled = true;
            txtUsername.Enabled = true;
        }
    }

    private async Task RunSboardLoginAsync(LoginLogForm logForm, string username, string uid, string upw)
    {
        var sessionWindows = _auto.FindAllWindowsByTitle("Sboard [");
        var loginWindows = _auto.FindAllWindowsByTitle("Sboard");

        bool foundSession = sessionWindows.Any(w => w.Title.StartsWith("Sboard ["));
        bool foundLogin = loginWindows.Any(w => w.Title.Trim() == "Sboard");

        if (foundSession)
        {
            logForm.AppendLog("기존 Sboard 세션 발견, 강제 종료 후 재시작...");
            _auto.KillSboard();
            await Task.Delay(500);
            logForm.AppendLog("Sboard 실행...");
            if (!_auto.LaunchSboard()) return;
        }
        else if (!foundLogin)
        {
            logForm.AppendLog("Sboard 실행...");
            if (!_auto.LaunchSboard()) return;
        }
        else
        {
            logForm.AppendLog("Sboard 로그인 창 확인됨");
        }

        await Task.Run(() => WaitForLoginWindow(logForm, uid, upw));
    }

    private void WaitForLoginWindow(LoginLogForm logForm, string uid, string upw)
    {
        IntPtr hwnd = IntPtr.Zero;
        var startTime = DateTime.Now;

        while ((DateTime.Now - startTime).TotalSeconds < 15)
        {
            hwnd = _auto.FindLoginWindow();
            if (hwnd != IntPtr.Zero) break;
            Thread.Sleep(200);
        }

        if (hwnd == IntPtr.Zero)
        {
            logForm.AppendLog("로그인 창 탐색 실패");
            logForm.AppendLog("Sboard 로그인 창을 찾지 못했습니다.");
            return;
        }

        logForm.AppendLog("로그인 창 발견");
        Thread.Sleep(500);
        logForm.AppendLog("정보 입력 중...");
        _auto.InputCredentials(hwnd, uid, upw);
        logForm.AppendLog("로그인 진행 중...");

        Thread.Sleep(1000);
        CheckLoginResult(logForm, hwnd);
    }

    private void CheckLoginResult(LoginLogForm logForm, IntPtr loginHwnd)
    {
        var startTime = DateTime.Now;
        uint pid = _auto.GetProcessId(loginHwnd);

        while ((DateTime.Now - startTime).TotalSeconds < 6)
        {
            var session = _auto.FindSessionWindow();
            if (session != IntPtr.Zero)
            {
                logForm.AppendLog("로그인 성공!");
                logForm.AppendLog("로그인이 완료되었습니다.");
                logForm.AppendLog("Enter를 누르시면 창이 종료됩니다.");
                return;
            }

            var errorDlg = _auto.DetectErrorDialog(pid);
            if (errorDlg != IntPtr.Zero)
            {
                logForm.AppendLog("로그인 실패 (정보 불일치)");
                logForm.AppendLog("로그인 정보가 일치하지 않습니다.");
                return;
            }

            Thread.Sleep(200);
        }

        logForm.AppendLog("로그인 실패 (시간 초과)");
        logForm.AppendLog("로그인 정보가 일치하지 않습니다.");
    }

    private void BtnManage_Click(object? sender, EventArgs e)
    {
        using var dlg = new Form
        {
            Text = "사용자 관리",
            Size = new Size(280, 220),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MinimizeBox = false,
            MaximizeBox = false,
            BackColor = Color.White,
            Font = new Font("Consolas", 10F)
        };

        var lo = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(15, 12, 15, 12),
            ColumnCount = 1
        };

        void AddBtn(string text, EventHandler click)
        {
            var btn = new Button
            {
                Text = text,
                Dock = DockStyle.Fill,
                Height = 36,
                Margin = new Padding(0, 0, 0, 6),
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                BackColor = Color.FromArgb(52, 152, 219),
                ForeColor = Color.White,
                Font = new Font("Consolas", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btn.MouseEnter += (_, _) => btn.BackColor = Color.FromArgb(41, 128, 185);
            btn.MouseLeave += (_, _) => btn.BackColor = Color.FromArgb(52, 152, 219);
            btn.Click += click;
            lo.Controls.Add(btn);
        }

        AddBtn("사용자 등록", async (_, _) => { dlg.Close(); await ShowRegisterDialogAsync(); });
        AddBtn("PW 변경", async (_, _) => { dlg.Close(); await ShowChangePwDialogAsync(); });
        AddBtn("사용자 삭제", async (_, _) => { dlg.Close(); await ShowDeleteDialogAsync(); });

        dlg.Controls.Add(lo);
        dlg.ShowDialog(this);
    }

    private async Task ShowRegisterDialogAsync()
    {
        var input = new InputDialog("사용자 등록", ("이름", "name"), ("ID", "uid"), ("PW", "pw"));
        var res = input.ShowDialog(this);
        if (res == null) return;

        try
        {
            await _api.CreateUserAsync(res["name"], res["uid"], res["pw"]);
            MessageBox.Show(this, "등록 완료!", "성공", MessageBoxButtons.OK, MessageBoxIcon.Information);
            _ = LoadUsersAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task ShowChangePwDialogAsync()
    {
        var verify = new InputDialog("PW 변경 - 본인 확인", ("이름", "name"), ("ID", "uid"), ("현재 PW", "pw"));
        var res = verify.ShowDialog(this);
        if (res == null) return;

        string name = res["name"], uid = res["uid"], pw = res["pw"];

        try
        {
            var users = await _api.GetUsersAsync();
            if (!users.TryGetValue(name, out var info) || info.Id != uid || info.Pw != pw)
            {
                MessageBox.Show(this, "사용자 정보가 일치하지 않습니다.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"서버 연결 실패: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        var newPwInput = new InputDialog("PW 변경 - 새 비밀번호", ("새 PW", "new_pw"));
        var res2 = newPwInput.ShowDialog(this);
        if (res2 == null || string.IsNullOrEmpty(res2["new_pw"])) return;

        try
        {
            await _api.UpdateUserPwOnlyAsync(name, uid, res2["new_pw"]);
            MessageBox.Show(this, "변경 완료!", "성공", MessageBoxButtons.OK, MessageBoxIcon.Information);
            _ = LoadUsersAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task ShowDeleteDialogAsync()
    {
        try
        {
            _users = await _api.GetUsersAsync();
            RefreshUserGrid();
        }
        catch { }

        var input = new InputDialog("사용자 삭제", ("이름", "name"), ("ID", "uid"), ("PW", "pw"));
        var res = input.ShowDialog(this);
        if (res == null) return;

        string name = res["name"], uid = res["uid"], pw = res["pw"];

        if (!_users.TryGetValue(name, out var info) || info.Id != uid || info.Pw != pw)
        {
            MessageBox.Show(this, "사용자 정보가 일치하지 않습니다.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (MessageBox.Show(this, $"{name} 님을 삭제하시겠습니까?", "확인",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        try
        {
            await _api.DeleteUserAsync(name);
            MessageBox.Show(this, "삭제 완료!", "성공", MessageBoxButtons.OK, MessageBoxIcon.Information);
            _ = LoadUsersAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
