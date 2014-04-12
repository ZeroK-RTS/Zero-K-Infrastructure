namespace ZeroKLobby
{
    partial class NavigationControl
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(NavigationControl));
            this.urlBox = new System.Windows.Forms.TextBox();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.logoutButton = new ZeroKLobby.BitmapButton();
            this.isBusyIcon = new System.Windows.Forms.PictureBox();
            this.goButton1 = new ZeroKLobby.BitmapButton();
            this.reloadButton1 = new ZeroKLobby.BitmapButton();
            this.btnForward = new ZeroKLobby.BitmapButton();
            this.btnBack = new ZeroKLobby.BitmapButton();
            this.tabControl = new ZeroKLobby.HeadlessTabControl();
            this.flowLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.isBusyIcon)).BeginInit();
            this.SuspendLayout();
            // 
            // urlBox
            // 
            this.urlBox.Location = new System.Drawing.Point(166, 34);
            this.urlBox.Name = "urlBox";
            this.urlBox.Size = new System.Drawing.Size(190, 20);
            this.urlBox.TabIndex = 2;
            this.urlBox.Enter += new System.EventHandler(this.urlBox_Enter);
            this.urlBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.urlBox_KeyDown);
            this.urlBox.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.urlBox_MouseDoubleClick);
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.AutoScroll = true;
            this.flowLayoutPanel1.AutoSize = true;
            this.flowLayoutPanel1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flowLayoutPanel1.BackColor = System.Drawing.Color.DimGray;
            this.flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.flowLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.flowLayoutPanel1.MinimumSize = new System.Drawing.Size(300, 28);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(703, 31);
            this.flowLayoutPanel1.TabIndex = 5;
            // 
            // logoutButton
            // 
            this.logoutButton.BackColor = System.Drawing.Color.Transparent;
            this.logoutButton.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("logoutButton.BackgroundImage")));
            this.logoutButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.logoutButton.Cursor = System.Windows.Forms.Cursors.Hand;
            this.logoutButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.logoutButton.ForeColor = System.Drawing.Color.White;
            this.logoutButton.Image = global::ZeroKLobby.Buttons.logout;
            this.logoutButton.Margin = new System.Windows.Forms.Padding(0, 0, 0, 3);
            this.logoutButton.Name = "logoutButton";
            this.logoutButton.Size = new System.Drawing.Size(96, 32);
            this.logoutButton.TabIndex = 8;
            this.logoutButton.Text = "LOGOUT";
            this.logoutButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.logoutButton.UseVisualStyleBackColor = true;
            this.logoutButton.Click += new System.EventHandler(this.logoutButton_Click);
            // 
            // isBusyIcon
            // 
            this.isBusyIcon.Image = ((System.Drawing.Image)(resources.GetObject("isBusyIcon.Image")));
            this.isBusyIcon.Location = new System.Drawing.Point(468, 36);
            this.isBusyIcon.Name = "isBusyIcon";
            this.isBusyIcon.Size = new System.Drawing.Size(25, 20);
            this.isBusyIcon.TabIndex = 8;
            this.isBusyIcon.TabStop = false;
            this.isBusyIcon.Visible = false;
            // 
            // goButton1
            // 
            this.goButton1.BackColor = System.Drawing.Color.Transparent;
            this.goButton1.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("goButton1.BackgroundImage")));
            this.goButton1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.goButton1.Cursor = System.Windows.Forms.Cursors.Hand;
            this.goButton1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.goButton1.ForeColor = System.Drawing.Color.White;
            this.goButton1.Location = new System.Drawing.Point(362, 34);
            this.goButton1.Name = "goButton1";
            this.goButton1.Size = new System.Drawing.Size(35, 23);
            this.goButton1.TabIndex = 6;
            this.goButton1.Text = "Go";
            this.goButton1.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.goButton1.UseVisualStyleBackColor = true;
            this.goButton1.Click += new System.EventHandler(this.goButton1_Click);
            // 
            // reloadButton1
            // 
            this.reloadButton1.BackColor = System.Drawing.Color.Transparent;
            this.reloadButton1.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("reloadButton1.BackgroundImage")));
            this.reloadButton1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.reloadButton1.Cursor = System.Windows.Forms.Cursors.Hand;
            this.reloadButton1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.reloadButton1.ForeColor = System.Drawing.Color.White;
            this.reloadButton1.Location = new System.Drawing.Point(403, 34);
            this.reloadButton1.Name = "reloadButton1";
            this.reloadButton1.Size = new System.Drawing.Size(58, 23);
            this.reloadButton1.TabIndex = 7;
            this.reloadButton1.Text = "Refresh";
            this.reloadButton1.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.reloadButton1.UseVisualStyleBackColor = true;
            this.reloadButton1.Visible = false;
            this.reloadButton1.Click += new System.EventHandler(this.reloadButton1_Click);
            // 
            // btnForward
            // 
            this.btnForward.BackColor = System.Drawing.Color.Transparent;
            this.btnForward.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("btnForward.BackgroundImage")));
            this.btnForward.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btnForward.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnForward.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnForward.ForeColor = System.Drawing.Color.White;
            this.btnForward.Location = new System.Drawing.Point(85, 34);
            this.btnForward.Name = "btnForward";
            this.btnForward.Size = new System.Drawing.Size(75, 23);
            this.btnForward.TabIndex = 4;
            this.btnForward.Text = "Forward";
            this.btnForward.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnForward.UseVisualStyleBackColor = true;
            this.btnForward.Click += new System.EventHandler(this.btnForward_Click);
            // 
            // btnBack
            // 
            this.btnBack.BackColor = System.Drawing.Color.Transparent;
            this.btnBack.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("btnBack.BackgroundImage")));
            this.btnBack.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btnBack.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnBack.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBack.ForeColor = System.Drawing.Color.White;
            this.btnBack.Location = new System.Drawing.Point(4, 34);
            this.btnBack.Name = "btnBack";
            this.btnBack.Size = new System.Drawing.Size(75, 23);
            this.btnBack.TabIndex = 3;
            this.btnBack.Text = "Back";
            this.btnBack.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnBack.UseVisualStyleBackColor = true;
            this.btnBack.Click += new System.EventHandler(this.btnBack_Click);
            // 
            // tabControl
            // 
            this.tabControl.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControl.Location = new System.Drawing.Point(0, 42);
            this.tabControl.Name = "tabControl";
            this.tabControl.SelectedIndex = 0;
            this.tabControl.Size = new System.Drawing.Size(703, 185);
            this.tabControl.TabIndex = 0;
            this.tabControl.Selecting += new System.Windows.Forms.TabControlCancelEventHandler(this.tabControl_Selecting);
            // 
            // NavigationControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.BackColor = System.Drawing.Color.DimGray;
            this.Controls.Add(this.isBusyIcon);
            this.Controls.Add(this.goButton1);
            this.Controls.Add(this.reloadButton1);
            this.Controls.Add(this.flowLayoutPanel1);
            this.Controls.Add(this.btnForward);
            this.Controls.Add(this.btnBack);
            this.Controls.Add(this.urlBox);
            this.Controls.Add(this.tabControl);
            this.Name = "NavigationControl";
            this.Size = new System.Drawing.Size(703, 219);
            this.Resize += new System.EventHandler(this.NavigationControl_Resize);
            this.flowLayoutPanel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.isBusyIcon)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private ZeroKLobby.HeadlessTabControl tabControl;
        private System.Windows.Forms.TextBox urlBox;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private BitmapButton btnBack;
        private BitmapButton btnForward;
        private BitmapButton reloadButton1;
        private BitmapButton goButton1;
        public System.Windows.Forms.PictureBox isBusyIcon;
        private BitmapButton logoutButton;
    }
}
