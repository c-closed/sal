namespace Sboard접속기.Forms;

public partial class InputDialog : Form
{
    private readonly Dictionary<string, Control> _inputs = [];
    private readonly Button _btnOk;
    private bool _ok;

    public InputDialog(string title, params (string Label, string Key)[] fields)
    {
        this.Text = title;
        this.Size = new Size(340, 100 + fields.Length * 40);
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

        _btnOk = new Button { Text = "확인", DialogResult = DialogResult.OK, Dock = DockStyle.Fill };
        var btnCancel = new Button { Text = "취소", DialogResult = DialogResult.Cancel, Dock = DockStyle.Fill };
        _btnOk.Click += (_, _) => _ok = true;
        var btnPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0)
        };
        btnPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        btnPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        lo.SetColumnSpan(btnPanel, 2);
        lo.SetRow(btnPanel, fields.Length);
        btnPanel.Controls.Add(btnCancel, 0, 0);
        btnPanel.Controls.Add(_btnOk, 1, 0);
        lo.Controls.Add(btnPanel);

        this.AcceptButton = _btnOk;
        this.Controls.Add(lo);
        LoadIcon();
        ApplyModernStyle();
    }

    private void ApplyModernStyle()
    {
        BackColor = Color.White;
        ForeColor = Color.FromArgb(52, 73, 94);

        foreach (Control c in this.Controls)
        {
            if (c is TableLayoutPanel tbl)
            {
                foreach (Control child in tbl.Controls)
                {
                    if (child is Label lbl)
                    {
                        lbl.Font = new Font("Consolas", 12F);
                        lbl.ForeColor = Color.FromArgb(52, 73, 94);
                    }
                    if (child is TextBox tb)
                    {
                        tb.Font = new Font("Consolas", 12F);
                        tb.BorderStyle = BorderStyle.FixedSingle;
                        tb.BackColor = Color.White;
                    }
                    if (child is TableLayoutPanel flp)
                    {
                        foreach (Control btn in flp.Controls)
                        {
                            if (btn is Button b)
                                StyleButton(b);
                        }
                    }
                }
            }
        }
    }

    private static void StyleButton(Button btn)
    {
        btn.FlatStyle = FlatStyle.Flat;
        btn.FlatAppearance.BorderSize = 0;
        btn.Font = new Font("Consolas", 12F, FontStyle.Bold);
        btn.Cursor = Cursors.Hand;
        if (btn.Text == "확인")
        {
            btn.BackColor = Color.FromArgb(52, 152, 219);
            btn.ForeColor = Color.White;
            btn.MouseEnter += (_, _) => btn.BackColor = Color.FromArgb(41, 128, 185);
            btn.MouseLeave += (_, _) => btn.BackColor = Color.FromArgb(52, 152, 219);
        }
        else
        {
            btn.BackColor = Color.FromArgb(149, 165, 166);
            btn.ForeColor = Color.White;
            btn.MouseEnter += (_, _) => btn.BackColor = Color.FromArgb(127, 140, 141);
            btn.MouseLeave += (_, _) => btn.BackColor = Color.FromArgb(149, 165, 166);
        }
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
