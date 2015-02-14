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
            this.navigationControl1 = new ZeroKLobby.NavigationControl();
            this.notifySection1 = new SpringDownloader.Notifications.NotifySection();
            this.btnWindowed = new ZeroKLobby.BitmapButton();
            this.btnSnd = new ZeroKLobby.BitmapButton();
            this.switchPanel1 = new ZeroKLobby.Controls.SwitchPanel();
            this.SuspendLayout();
            // 
            // navigationControl1
            // 
            this.navigationControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.navigationControl1.BackColor = System.Drawing.Color.DimGray;
            this.navigationControl1.Location = new System.Drawing.Point(675, 22);
            this.navigationControl1.Margin = new System.Windows.Forms.Padding(0);
            this.navigationControl1.Name = "navigationControl1";
            this.navigationControl1.Path = "";
            this.navigationControl1.Size = new System.Drawing.Size(664, 885);
            this.navigationControl1.TabIndex = 1;
            // 
            // notifySection1
            // 
            this.notifySection1.AutoSize = true;
            this.notifySection1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.notifySection1.BackColor = System.Drawing.Color.Transparent;
            this.notifySection1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.notifySection1.Location = new System.Drawing.Point(0, 916);
            this.notifySection1.Margin = new System.Windows.Forms.Padding(0);
            this.notifySection1.Name = "notifySection1";
            this.notifySection1.Size = new System.Drawing.Size(1339, 0);
            this.notifySection1.TabIndex = 0;
            // 
            // btnWindowed
            // 
            this.btnWindowed.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnWindowed.BackColor = System.Drawing.Color.Transparent;
            this.btnWindowed.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("btnWindowed.BackgroundImage")));
            this.btnWindowed.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.btnWindowed.ButtonStyle = ZeroKLobby.ButtonRenderer.StyleType.DarkHive;
            this.btnWindowed.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnWindowed.FlatAppearance.BorderSize = 0;
            this.btnWindowed.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnWindowed.ForeColor = System.Drawing.Color.White;
            this.btnWindowed.Image = global::ZeroKLobby.Buttons.win_minmax;
            this.btnWindowed.Location = new System.Drawing.Point(6, 857);
            this.btnWindowed.Name = "btnWindowed";
            this.btnWindowed.Size = new System.Drawing.Size(50, 50);
            this.btnWindowed.SoundType = ZeroKLobby.Controls.SoundPalette.SoundType.Click;
            this.btnWindowed.TabIndex = 7;
            this.btnWindowed.UseVisualStyleBackColor = false;
            this.btnWindowed.Click += new System.EventHandler(this.btnWindowed_Click);
            // 
            // btnSnd
            // 
            this.btnSnd.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnSnd.BackColor = System.Drawing.Color.Transparent;
            this.btnSnd.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("btnSnd.BackgroundImage")));
            this.btnSnd.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.btnSnd.ButtonStyle = ZeroKLobby.ButtonRenderer.StyleType.DarkHive;
            this.btnSnd.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSnd.FlatAppearance.BorderSize = 0;
            this.btnSnd.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSnd.ForeColor = System.Drawing.Color.White;
            this.btnSnd.Image = global::ZeroKLobby.Buttons.snd;
            this.btnSnd.Location = new System.Drawing.Point(77, 857);
            this.btnSnd.Name = "btnSnd";
            this.btnSnd.Size = new System.Drawing.Size(50, 50);
            this.btnSnd.SoundType = ZeroKLobby.Controls.SoundPalette.SoundType.Click;
            this.btnSnd.TabIndex = 8;
            this.btnSnd.UseVisualStyleBackColor = false;
            this.btnSnd.Click += new System.EventHandler(this.btnSnd_Click);
            // 
            // switchPanel1
            // 
            this.switchPanel1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.switchPanel1.BackColor = System.Drawing.Color.Transparent;
            this.switchPanel1.Location = new System.Drawing.Point(0, 69);
            this.switchPanel1.Name = "switchPanel1";
            this.switchPanel1.Size = new System.Drawing.Size(1339, 782);
            this.switchPanel1.TabIndex = 9;
            // 
            // MainWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.BackgroundImage = global::ZeroKLobby.BgImages.bg_battle;
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.ClientSize = new System.Drawing.Size(1339, 916);
            this.Controls.Add(this.navigationControl1);
            this.Controls.Add(this.switchPanel1);
            this.Controls.Add(this.btnWindowed);
            this.Controls.Add(this.btnSnd);
            this.Controls.Add(this.notifySection1);
            this.DoubleBuffered = true;
            this.Font = new System.Drawing.Font("Comic Sans MS", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ForeColor = System.Drawing.Color.White;
            this.Name = "MainWindow";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainWindow_FormClosing);
            this.Load += new System.EventHandler(this.MainWindow_Load);
            this.SizeChanged += new System.EventHandler(this.Window_StateChanged);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private NavigationControl navigationControl1;
        private SpringDownloader.Notifications.NotifySection notifySection1;
        private BitmapButton btnWindowed;
        private BitmapButton btnSnd;
        private Controls.SwitchPanel switchPanel1;

    }
}