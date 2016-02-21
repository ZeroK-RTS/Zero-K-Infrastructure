namespace ZeroKLobby.MainPages
{
    partial class HomePage
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(HomePage));
            this.singleplayerButton = new ZeroKLobby.BitmapButton();
            this.exitButton = new ZeroKLobby.BitmapButton();
            this.multiplayerButton = new ZeroKLobby.BitmapButton();
            this.SuspendLayout();
            // 
            // singleplayerButton
            // 
            this.singleplayerButton.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.singleplayerButton.BackColor = System.Drawing.Color.Transparent;
            this.singleplayerButton.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("singleplayerButton.BackgroundImage")));
            this.singleplayerButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.singleplayerButton.ButtonStyle = ZeroKLobby.FrameBorderRenderer.StyleType.DarkHive;
            this.singleplayerButton.Cursor = System.Windows.Forms.Cursors.Hand;
            this.singleplayerButton.FlatAppearance.BorderSize = 0;
            this.singleplayerButton.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Transparent;
            this.singleplayerButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Transparent;
            this.singleplayerButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.singleplayerButton.ForeColor = System.Drawing.Color.White;
            this.singleplayerButton.Image = global::ZeroKLobby.Buttons.sp;
            this.singleplayerButton.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.singleplayerButton.Location = new System.Drawing.Point(33, 277);
            this.singleplayerButton.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.singleplayerButton.Name = "singleplayerButton";
            this.singleplayerButton.Size = new System.Drawing.Size(375, 77);
            this.singleplayerButton.SoundType = ZeroKLobby.Controls.SoundPalette.SoundType.Servo;
            this.singleplayerButton.TabIndex = 10;
            this.singleplayerButton.Text = "Singleplayer";
            this.singleplayerButton.UseVisualStyleBackColor = false;
            this.singleplayerButton.Click += new System.EventHandler(this.singleplayerButton_Click);
            // 
            // exitButton
            // 
            this.exitButton.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.exitButton.BackColor = System.Drawing.Color.Transparent;
            this.exitButton.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("exitButton.BackgroundImage")));
            this.exitButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.exitButton.ButtonStyle = ZeroKLobby.FrameBorderRenderer.StyleType.DarkHive;
            this.exitButton.Cursor = System.Windows.Forms.Cursors.Hand;
            this.exitButton.FlatAppearance.BorderSize = 0;
            this.exitButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.exitButton.ForeColor = System.Drawing.Color.White;
            this.exitButton.Image = global::ZeroKLobby.Buttons.exit;
            this.exitButton.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.exitButton.Location = new System.Drawing.Point(33, 586);
            this.exitButton.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.exitButton.Name = "exitButton";
            this.exitButton.Size = new System.Drawing.Size(375, 77);
            this.exitButton.SoundType = ZeroKLobby.Controls.SoundPalette.SoundType.Click;
            this.exitButton.TabIndex = 9;
            this.exitButton.Text = "Exit";
            this.exitButton.UseVisualStyleBackColor = false;
            this.exitButton.Click += new System.EventHandler(this.exitButton_Click);
            // 
            // multiplayerButton
            // 
            this.multiplayerButton.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.multiplayerButton.BackColor = System.Drawing.Color.Transparent;
            this.multiplayerButton.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("multiplayerButton.BackgroundImage")));
            this.multiplayerButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.multiplayerButton.ButtonStyle = ZeroKLobby.FrameBorderRenderer.StyleType.DarkHive;
            this.multiplayerButton.Cursor = System.Windows.Forms.Cursors.Hand;
            this.multiplayerButton.FlatAppearance.BorderSize = 0;
            this.multiplayerButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.multiplayerButton.ForeColor = System.Drawing.Color.White;
            this.multiplayerButton.Image = global::ZeroKLobby.Buttons.mp;
            this.multiplayerButton.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.multiplayerButton.Location = new System.Drawing.Point(33, 426);
            this.multiplayerButton.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.multiplayerButton.Name = "multiplayerButton";
            this.multiplayerButton.Size = new System.Drawing.Size(375, 77);
            this.multiplayerButton.SoundType = ZeroKLobby.Controls.SoundPalette.SoundType.Servo;
            this.multiplayerButton.TabIndex = 8;
            this.multiplayerButton.Text = "Multiplayer";
            this.multiplayerButton.UseVisualStyleBackColor = false;
            this.multiplayerButton.Click += new System.EventHandler(this.multiplayerButton_Click);
            // 
            // HomePage
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.Transparent;
            this.Controls.Add(this.singleplayerButton);
            this.Controls.Add(this.exitButton);
            this.Controls.Add(this.multiplayerButton);
            this.DoubleBuffered = true;
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.Name = "HomePage";
            this.Size = new System.Drawing.Size(1546, 949);
            this.ResumeLayout(false);

        }

        #endregion

        private BitmapButton singleplayerButton;
        private BitmapButton exitButton;
        private BitmapButton multiplayerButton;
    }
}
