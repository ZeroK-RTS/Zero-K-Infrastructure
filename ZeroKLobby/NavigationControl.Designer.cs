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
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.logoutButton = new ZeroKLobby.BitmapButton();
            this.tabControl = new ZeroKLobby.HeadlessTabControl();
            this.SuspendLayout();
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.AutoScroll = true;
            this.flowLayoutPanel1.AutoSize = true;
            this.flowLayoutPanel1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flowLayoutPanel1.BackColor = System.Drawing.Color.DimGray;
            this.flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.flowLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.flowLayoutPanel1.Margin = new System.Windows.Forms.Padding(0);
            this.flowLayoutPanel1.MinimumSize = new System.Drawing.Size(300, 28);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(703, 28);
            this.flowLayoutPanel1.TabIndex = 5;
            this.flowLayoutPanel1.WrapContents = false;
            // 
            // logoutButton
            // 
            this.logoutButton.BackColor = System.Drawing.Color.Transparent;
            this.logoutButton.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("logoutButton.BackgroundImage")));
            this.logoutButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.logoutButton.ButtonStyle = ZeroKLobby.ButtonRenderer.StyleType.DarkHive;
            this.logoutButton.Cursor = System.Windows.Forms.Cursors.Hand;
            this.logoutButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.logoutButton.ForeColor = System.Drawing.Color.White;
            this.logoutButton.Image = global::ZeroKLobby.Buttons.logout;
            this.logoutButton.Location = new System.Drawing.Point(0, 0);
            this.logoutButton.Margin = new System.Windows.Forms.Padding(0, 0, 0, 3);
            this.logoutButton.Name = "logoutButton";
            this.logoutButton.Size = new System.Drawing.Size(96, 32);
            this.logoutButton.SoundType = ZeroKLobby.Controls.SoundPalette.SoundType.Click;
            this.logoutButton.TabIndex = 8;
            this.logoutButton.Text = "LOGOUT";
            this.logoutButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.logoutButton.UseVisualStyleBackColor = true;
            this.logoutButton.Click += new System.EventHandler(this.logoutButton_Click);
            // 
            // tabControl
            // 
            this.tabControl.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControl.Location = new System.Drawing.Point(0, 42);
            this.tabControl.Margin = new System.Windows.Forms.Padding(0);
            this.tabControl.Name = "tabControl";
            this.tabControl.Padding = new System.Drawing.Point(0, 0);
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
            this.Controls.Add(this.flowLayoutPanel1);
            this.Controls.Add(this.tabControl);
            this.Margin = new System.Windows.Forms.Padding(0);
            this.Name = "NavigationControl";
            this.Size = new System.Drawing.Size(703, 219);
            this.Resize += new System.EventHandler(this.NavigationControl_Resize);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private ZeroKLobby.HeadlessTabControl tabControl;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private BitmapButton logoutButton;
    }
}
