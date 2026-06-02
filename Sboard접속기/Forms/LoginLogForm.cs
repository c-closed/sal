namespace Sboard접속기.Forms;

public partial class LoginLogForm : Form
{
    public LoginLogForm(string username)
    {
        InitializeComponent();
        this.Text = $"{username} 로그인 진행";
        LoadIcon();
        this.KeyDown += LoginLogForm_KeyDown;
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

    public void AppendLog(string msg)
    {
        if (this.IsDisposed) return;
        if (this.InvokeRequired)
        {
            this.Invoke(() => AppendLog(msg));
            return;
        }
        txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {msg}\r\n");
    }

    private void LoginLogForm_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter) this.Close();
    }
}
