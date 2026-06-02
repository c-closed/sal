namespace Sboard접속기.Forms;

public partial class InputDialog : Form
{
    private readonly Dictionary<string, Control> _inputs = [];
    private bool _ok;

    public InputDialog(string title, params (string Label, string Key)[] fields)
    {
        this.Text = title;
        this.Size = new Size(300, 100 + fields.Length * 40);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MinimizeBox = false;
        this.MaximizeBox = false;

        var lo = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            ColumnCount = 2,
            RowCount = fields.Length + 1,
            AutoSize = true
        };

        int row = 0;
        foreach (var (label, key) in fields)
        {
            lo.Controls.Add(new Label { Text = label, TextAlign = ContentAlignment.MiddleLeft, Anchor = AnchorStyles.Left }, 0, row);
            var tb = new TextBox { Anchor = AnchorStyles.Left | AnchorStyles.Right };
            _inputs[key] = tb;
            lo.Controls.Add(tb, 1, row);
            row++;
        }

        var btnOk = new Button { Text = "확인", DialogResult = DialogResult.OK, Anchor = AnchorStyles.None };
        var btnCancel = new Button { Text = "취소", DialogResult = DialogResult.Cancel, Anchor = AnchorStyles.None };
        btnOk.Click += (_, _) => _ok = true;
        var btnPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill };
        lo.SetColumnSpan(btnPanel, 2);
        lo.SetRow(btnPanel, fields.Length);
        btnPanel.Controls.Add(btnCancel);
        btnPanel.Controls.Add(btnOk);
        lo.Controls.Add(btnPanel);

        this.Controls.Add(lo);
        LoadIcon();
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

    public Dictionary<string, string>? ShowDialog(Form owner)
    {
        _ok = false;
        if (base.ShowDialog(owner) == DialogResult.OK && _ok)
            return _inputs.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Text);
        return null;
    }

    private void InitializeComponent()
    {

    }
}
