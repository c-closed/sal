namespace Sboard접속기.Forms;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
        DataGridViewCellStyle dataGridViewCellStyle2 = new DataGridViewCellStyle();
        System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
        tlpMain = new TableLayoutPanel();
        gbLogin = new GroupBox();
        txtUsername = new TextBox();
        btnLogin = new Button();
        btnManage = new Button();
        gbUsers = new GroupBox();
        dgvUsers = new DataGridView();
        colName = new DataGridViewTextBoxColumn();
        colId = new DataGridViewTextBoxColumn();
        lblStatus = new Label();
        tlpMain.SuspendLayout();
        gbLogin.SuspendLayout();
        gbUsers.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)dgvUsers).BeginInit();
        SuspendLayout();
        // 
        // tlpMain
        // 
        tlpMain.ColumnCount = 1;
        tlpMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        tlpMain.Controls.Add(gbLogin, 0, 0);
        tlpMain.Controls.Add(gbUsers, 0, 1);
        tlpMain.Dock = DockStyle.Fill;
        tlpMain.Location = new Point(0, 0);
        tlpMain.Name = "tlpMain";
        tlpMain.Padding = new Padding(12, 12, 12, 8);
        tlpMain.RowCount = 2;
        tlpMain.RowStyles.Add(new RowStyle());
        tlpMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        tlpMain.Size = new Size(336, 420);
        tlpMain.TabIndex = 0;
        // 
        // gbLogin
        // 
        gbLogin.AutoSize = true;
        gbLogin.Controls.Add(txtUsername);
        gbLogin.Controls.Add(btnLogin);
        gbLogin.Controls.Add(btnManage);
        gbLogin.Dock = DockStyle.Fill;
        gbLogin.Location = new Point(12, 12);
        gbLogin.Margin = new Padding(0);
        gbLogin.Name = "gbLogin";
        gbLogin.Padding = new Padding(8, 16, 8, 8);
        gbLogin.Size = new Size(312, 120);
        gbLogin.TabIndex = 0;
        gbLogin.TabStop = false;
        gbLogin.Text = "자동 로그인";
        // 
        // txtUsername
        // 
        txtUsername.Location = new Point(16, 36);
        txtUsername.Name = "txtUsername";
        txtUsername.Size = new Size(280, 23);
        txtUsername.TabIndex = 0;
        // 
        // btnLogin
        // 
        btnLogin.Location = new Point(16, 65);
        btnLogin.Name = "btnLogin";
        btnLogin.Size = new Size(132, 28);
        btnLogin.TabIndex = 1;
        btnLogin.Text = "로그인";
        btnLogin.UseVisualStyleBackColor = true;
        // 
        // btnManage
        // 
        btnManage.Location = new Point(154, 65);
        btnManage.Name = "btnManage";
        btnManage.Size = new Size(142, 28);
        btnManage.TabIndex = 2;
        btnManage.Text = "사용자 관리";
        btnManage.UseVisualStyleBackColor = true;
        // 
        // gbUsers
        // 
        gbUsers.Controls.Add(dgvUsers);
        gbUsers.Controls.Add(lblStatus);
        gbUsers.Dock = DockStyle.Fill;
        gbUsers.Location = new Point(12, 132);
        gbUsers.Margin = new Padding(0, 0, 0, 6);
        gbUsers.Name = "gbUsers";
        gbUsers.Padding = new Padding(8, 16, 8, 8);
        gbUsers.Size = new Size(312, 274);
        gbUsers.TabIndex = 1;
        gbUsers.TabStop = false;
        gbUsers.Text = "사용자 목록";
        // 
        // dgvUsers
        // 
        dgvUsers.AllowUserToAddRows = false;
        dgvUsers.AllowUserToDeleteRows = false;
        dgvUsers.AllowUserToResizeRows = false;
        dgvUsers.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        dgvUsers.BackgroundColor = SystemColors.Window;
        dgvUsers.BorderStyle = BorderStyle.Fixed3D;
        dgvUsers.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        dgvUsers.Columns.AddRange(new DataGridViewColumn[] { colName, colId });
        dgvUsers.Dock = DockStyle.Fill;
        dgvUsers.Location = new Point(8, 32);
        dgvUsers.MultiSelect = false;
        dgvUsers.Name = "dgvUsers";
        dgvUsers.ReadOnly = true;
        dgvUsers.RowHeadersVisible = false;
        dgvUsers.RowTemplate.Height = 28;
        dgvUsers.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgvUsers.Size = new Size(296, 234);
        dgvUsers.TabIndex = 0;
        dgvUsers.CellContentClick += dgvUsers_CellContentClick;
        // 
        // colName
        // 
        colName.DataPropertyName = "Name";
        dataGridViewCellStyle1.Alignment = DataGridViewContentAlignment.MiddleCenter;
        colName.DefaultCellStyle = dataGridViewCellStyle1;
        colName.FillWeight = 50F;
        colName.HeaderText = "이름";
        colName.MinimumWidth = 80;
        colName.Name = "colName";
        colName.ReadOnly = true;
        colName.SortMode = DataGridViewColumnSortMode.NotSortable;
        // 
        // colId
        // 
        colId.DataPropertyName = "Id";
        dataGridViewCellStyle2.Alignment = DataGridViewContentAlignment.MiddleCenter;
        colId.DefaultCellStyle = dataGridViewCellStyle2;
        colId.FillWeight = 50F;
        colId.HeaderText = "ID";
        colId.MinimumWidth = 80;
        colId.Name = "colId";
        colId.ReadOnly = true;
        colId.SortMode = DataGridViewColumnSortMode.NotSortable;
        // 
        // lblStatus
        // 
        lblStatus.Dock = DockStyle.Fill;
        lblStatus.Font = new Font("Consolas", 9F);
        lblStatus.ForeColor = SystemColors.GrayText;
        lblStatus.Location = new Point(8, 32);
        lblStatus.Name = "lblStatus";
        lblStatus.Size = new Size(296, 234);
        lblStatus.TabIndex = 1;
        lblStatus.Text = "사용자 목록을 불러오는 중...";
        lblStatus.TextAlign = ContentAlignment.MiddleCenter;
        // 
        // MainForm
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(336, 420);
        Controls.Add(tlpMain);
        Icon = (Icon)resources.GetObject("$this.Icon");
        Name = "MainForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Sboard 접속기";
        tlpMain.ResumeLayout(false);
        tlpMain.PerformLayout();
        gbLogin.ResumeLayout(false);
        gbLogin.PerformLayout();
        gbUsers.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)dgvUsers).EndInit();
        ResumeLayout(false);
    }

    private System.Windows.Forms.TableLayoutPanel tlpMain;
    private System.Windows.Forms.GroupBox gbLogin;
    private System.Windows.Forms.TextBox txtUsername;
    private System.Windows.Forms.Button btnLogin;
    private System.Windows.Forms.Button btnManage;
    private System.Windows.Forms.GroupBox gbUsers;
    private System.Windows.Forms.DataGridView dgvUsers;
    private System.Windows.Forms.DataGridViewTextBoxColumn colName;
    private System.Windows.Forms.DataGridViewTextBoxColumn colId;
    private System.Windows.Forms.Label lblStatus;
}
