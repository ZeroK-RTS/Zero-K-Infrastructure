namespace ZeroKLobby
{
    partial class MainWindow
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainWindow));
            this.panelRight = new ZeroKLobby.Controls.RightPanel();
            this.btnHide = new ZeroKLobby.BitmapButton();
            this.lbRightPanelTitle = new System.Windows.Forms.Label();
            this.navigationControl1 = new ZeroKLobby.HeadlessTabControl();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.switchPanel1 = new ZeroKLobby.Controls.SwitchPanel();
            this.btnWindowed = new ZeroKLobby.BitmapButton();
            this.btnSnd = new ZeroKLobby.BitmapButton();
            this.notifySection1 = new SpringDownloader.Notifications.NotifySection();
            this.btnBack = new ZeroKLobby.BitmapButton();
            this.lbMainPageTitle = new System.Windows.Forms.Label();
            this.panelRight.SuspendLayout();
            this.SuspendLayout();
            // 
            // panelRight
            // 
            this.panelRight.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panelRight.BackColor = System.Drawing.Color.Transparent;
            this.panelRight.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("panelRight.BackgroundImage")));
            this.panelRight.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.panelRight.Controls.Add(this.btnHide);
            this.panelRight.Controls.Add(this.lbRightPanelTitle);
            this.panelRight.Controls.Add(this.navigationControl1);
            this.panelRight.Location = new System.Drawing.Point(602, 12);
            this.panelRight.Name = "panelRight";
            this.panelRight.Size = new System.Drawing.Size(772, 748);
            this.panelRight.TabIndex = 10;
            this.panelRight.Visible = false;
            // 
            // btnHide
            // 
            this.btnHide.BackColor = System.Drawing.Color.Transparent;
            this.btnHide.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("btnHide.BackgroundImage")));
            this.btnHide.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.btnHide.ButtonStyle = ZeroKLobby.FrameBorderRenderer.StyleType.DarkHive;
            this.btnHide.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnHide.FlatAppearance.BorderSize = 0;
            this.btnHide.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Transparent;
            this.btnHide.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Transparent;
            this.btnHide.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnHide.ForeColor = System.Drawing.Color.White;
            this.btnHide.Image = global::ZeroKLobby.Buttons.down;
            this.btnHide.Location = new System.Drawing.Point(21, 8);
            this.btnHide.Name = "btnHide";
            this.btnHide.Size = new System.Drawing.Size(37, 37);
            this.btnHide.SoundType = ZeroKLobby.Controls.SoundPalette.SoundType.Click;
            this.btnHide.TabIndex = 3;
            this.btnHide.UseVisualStyleBackColor = false;
            this.btnHide.Click += new System.EventHandler(this.btnHide_Click);
            // 
            // lbRightPanelTitle
            // 
            this.lbRightPanelTitle.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lbRightPanelTitle.Font = new System.Drawing.Font("Verdana", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.lbRightPanelTitle.ForeColor = System.Drawing.Color.White;
            this.lbRightPanelTitle.Location = new System.Drawing.Point(72, 14);
            this.lbRightPanelTitle.Name = "lbRightPanelTitle";
            this.lbRightPanelTitle.Size = new System.Drawing.Size(680, 25);
            this.lbRightPanelTitle.TabIndex = 2;
            this.lbRightPanelTitle.Text = "Chat";
            // 
            // navigationControl1
            // 
            this.navigationControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.navigationControl1.Location = new System.Drawing.Point(21, 28);
            this.navigationControl1.Margin = new System.Windows.Forms.Padding(0);
            this.navigationControl1.Name = "navigationControl1";
            this.navigationControl1.SelectedIndex = 0;
            this.navigationControl1.Size = new System.Drawing.Size(731, 702);
            this.navigationControl1.TabIndex = 1;
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.flowLayoutPanel1.BackColor = System.Drawing.Color.Transparent;
            this.flowLayoutPanel1.Location = new System.Drawing.Point(836, 760);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(355, 47);
            this.flowLayoutPanel1.TabIndex = 11;
            // 
            // switchPanel1
            // 
            this.switchPanel1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.switchPanel1.Location = new System.Drawing.Point(0, 63);
            this.switchPanel1.Name = "switchPanel1";
            this.switchPanel1.SelectedIndex = 0;
            this.switchPanel1.Size = new System.Drawing.Size(1386, 688);
            this.switchPanel1.TabIndex = 9;
            // 
            // btnWindowed
            // 
            this.btnWindowed.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnWindowed.BackColor = System.Drawing.Color.Transparent;
            this.btnWindowed.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("btnWindowed.BackgroundImage")));
            this.btnWindowed.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.btnWindowed.ButtonStyle = ZeroKLobby.FrameBorderRenderer.StyleType.DarkHive;
            this.btnWindowed.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnWindowed.FlatAppearance.BorderSize = 0;
            this.btnWindowed.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnWindowed.ForeColor = System.Drawing.Color.White;
            this.btnWindowed.Image = global::ZeroKLobby.Buttons.win_minmax;
            this.btnWindowed.Location = new System.Drawing.Point(1288, 769);
            this.btnWindowed.Name = "btnWindowed";
            this.btnWindowed.Size = new System.Drawing.Size(35, 35);
            this.btnWindowed.SoundType = ZeroKLobby.Controls.SoundPalette.SoundType.Click;
            this.btnWindowed.TabIndex = 7;
            this.btnWindowed.UseVisualStyleBackColor = false;
            this.btnWindowed.Click += new System.EventHandler(this.btnWindowed_Click);
            // 
            // btnSnd
            // 
            this.btnSnd.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSnd.BackColor = System.Drawing.Color.Transparent;
            this.btnSnd.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("btnSnd.BackgroundImage")));
            this.btnSnd.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.btnSnd.ButtonStyle = ZeroKLobby.FrameBorderRenderer.StyleType.DarkHive;
            this.btnSnd.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSnd.FlatAppearance.BorderSize = 0;
            this.btnSnd.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSnd.ForeColor = System.Drawing.Color.White;
            this.btnSnd.Image = global::ZeroKLobby.Buttons.snd;
            this.btnSnd.Location = new System.Drawing.Point(1339, 769);
            this.btnSnd.Name = "btnSnd";
            this.btnSnd.Size = new System.Drawing.Size(35, 35);
            this.btnSnd.SoundType = ZeroKLobby.Controls.SoundPalette.SoundType.Click;
            this.btnSnd.TabIndex = 8;
            this.btnSnd.UseVisualStyleBackColor = false;
            this.btnSnd.Click += new System.EventHandler(this.btnSnd_Click);
            // 
            // notifySection1
            // 
            this.notifySection1.AutoSize = true;
            this.notifySection1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.notifySection1.BackColor = System.Drawing.Color.Transparent;
            this.notifySection1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.notifySection1.Location = new System.Drawing.Point(0, 816);
            this.notifySection1.Margin = new System.Windows.Forms.Padding(0);
            this.notifySection1.Name = "notifySection1";
            this.notifySection1.Size = new System.Drawing.Size(1386, 0);
            this.notifySection1.TabIndex = 0;
            // 
            // btnBack
            // 
            this.btnBack.BackColor = System.Drawing.Color.Transparent;
            this.btnBack.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("btnBack.BackgroundImage")));
            this.btnBack.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.btnBack.ButtonStyle = ZeroKLobby.FrameBorderRenderer.StyleType.DarkHive;
            this.btnBack.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnBack.FlatAppearance.BorderSize = 0;
            this.btnBack.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Transparent;
            this.btnBack.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Transparent;
            this.btnBack.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBack.ForeColor = System.Drawing.Color.White;
            this.btnBack.Image = global::ZeroKLobby.Buttons.left;
            this.btnBack.Location = new System.Drawing.Point(12, 20);
            this.btnBack.Name = "btnBack";
            this.btnBack.Size = new System.Drawing.Size(37, 37);
            this.btnBack.SoundType = ZeroKLobby.Controls.SoundPalette.SoundType.Click;
            this.btnBack.TabIndex = 12;
            this.btnBack.UseVisualStyleBackColor = false;
            this.btnBack.Visible = false;
            this.btnBack.Click += new System.EventHandler(this.btnBack_Click);
            // 
            // lbMainPageTitle
            // 
            this.lbMainPageTitle.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lbMainPageTitle.BackColor = System.Drawing.Color.Transparent;
            this.lbMainPageTitle.Font = new System.Drawing.Font("Verdana", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.lbMainPageTitle.ForeColor = System.Drawing.Color.White;
            this.lbMainPageTitle.Location = new System.Drawing.Point(55, 26);
            this.lbMainPageTitle.Name = "lbMainPageTitle";
            this.lbMainPageTitle.Size = new System.Drawing.Size(541, 25);
            this.lbMainPageTitle.TabIndex = 13;
            this.lbMainPageTitle.Text = "Page title";
            // 
            // MainWindow
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.ClientSize = new System.Drawing.Size(1386, 816);
            this.Controls.Add(this.lbMainPageTitle);
            this.Controls.Add(this.btnBack);
            this.Controls.Add(this.flowLayoutPanel1);
            this.Controls.Add(this.panelRight);
            this.Controls.Add(this.switchPanel1);
            this.Controls.Add(this.btnWindowed);
            this.Controls.Add(this.btnSnd);
            this.Controls.Add(this.notifySection1);
            this.DoubleBuffered = true;
            this.Font = new System.Drawing.Font("Comic Sans MS", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Pixel, ((byte)(238)));
            this.ForeColor = System.Drawing.Color.White;
            this.Name = "MainWindow";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainWindow_FormClosing);
            this.Load += new System.EventHandler(this.MainWindow_Load);
            this.SizeChanged += new System.EventHandler(this.Window_StateChanged);
            this.panelRight.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private HeadlessTabControl navigationControl1;
        private SpringDownloader.Notifications.NotifySection notifySection1;
        private BitmapButton btnWindowed;
        private BitmapButton btnSnd;
        private Controls.SwitchPanel switchPanel1;
        private BitmapButton btnHide;
        public System.Windows.Forms.Label lbRightPanelTitle;
        public System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private BitmapButton btnBack;
        public System.Windows.Forms.Label lbMainPageTitle;
        public Controls.RightPanel panelRight;

    }
}