namespace Sboard접속기.Forms;

partial class LoginLogForm
{
    private void InitializeComponent()
    {
        System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LoginLogForm));
        txtLog = new TextBox();
        SuspendLayout();
        // 
        // txtLog
        // 
        txtLog.BackColor = Color.FromArgb(250, 250, 250);
        txtLog.Dock = DockStyle.Fill;
        txtLog.Font = new Font("Consolas", 8F);
        txtLog.Location = new Point(0, 0);
        txtLog.Multiline = true;
        txtLog.Name = "txtLog";
        txtLog.ReadOnly = true;
        txtLog.ScrollBars = ScrollBars.Vertical;
        txtLog.Size = new Size(380, 240);
        txtLog.TabIndex = 0;
        // 
        // LoginLogForm
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(380, 240);
        Controls.Add(txtLog);
        Icon = (Icon)resources.GetObject("$this.Icon");
        KeyPreview = true;
        Name = "LoginLogForm";
        StartPosition = FormStartPosition.CenterParent;
        TopMost = true;
        ResumeLayout(false);
        PerformLayout();
    }

    private System.Windows.Forms.TextBox txtLog;
}
