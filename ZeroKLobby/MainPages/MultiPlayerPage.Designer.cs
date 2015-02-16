namespace ZeroKLobby.MainPages
{
    partial class MultiPlayerPage
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MultiPlayerPage));
            this.btnSpectate = new ZeroKLobby.BitmapButton();
            this.btnCustomBattles = new ZeroKLobby.BitmapButton();
            this.btnJoinQueue = new ZeroKLobby.BitmapButton();
            this.SuspendLayout();
            // 
            // btnSpectate
            // 
            this.btnSpectate.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.btnSpectate.BackColor = System.Drawing.Color.Transparent;
            this.btnSpectate.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("btnSpectate.BackgroundImage")));
            this.btnSpectate.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.btnSpectate.ButtonStyle = ZeroKLobby.FrameBorderRenderer.StyleType.DarkHive;
            this.btnSpectate.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSpectate.FlatAppearance.BorderSize = 0;
            this.btnSpectate.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Transparent;
            this.btnSpectate.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Transparent;
            this.btnSpectate.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSpectate.ForeColor = System.Drawing.Color.White;
            this.btnSpectate.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnSpectate.Location = new System.Drawing.Point(36, 436);
            this.btnSpectate.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btnSpectate.Name = "btnSpectate";
            this.btnSpectate.Size = new System.Drawing.Size(375, 77);
            this.btnSpectate.SoundType = ZeroKLobby.Controls.SoundPalette.SoundType.Click;
            this.btnSpectate.TabIndex = 18;
            this.btnSpectate.Text = "Watch a battle";
            this.btnSpectate.UseVisualStyleBackColor = false;
            // 
            // btnCustomBattles
            // 
            this.btnCustomBattles.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.btnCustomBattles.BackColor = System.Drawing.Color.Transparent;
            this.btnCustomBattles.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("btnCustomBattles.BackgroundImage")));
            this.btnCustomBattles.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.btnCustomBattles.ButtonStyle = ZeroKLobby.FrameBorderRenderer.StyleType.DarkHive;
            this.btnCustomBattles.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnCustomBattles.FlatAppearance.BorderSize = 0;
            this.btnCustomBattles.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Transparent;
            this.btnCustomBattles.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Transparent;
            this.btnCustomBattles.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCustomBattles.ForeColor = System.Drawing.Color.White;
            this.btnCustomBattles.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnCustomBattles.Location = new System.Drawing.Point(36, 307);
            this.btnCustomBattles.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btnCustomBattles.Name = "btnCustomBattles";
            this.btnCustomBattles.Size = new System.Drawing.Size(375, 77);
            this.btnCustomBattles.SoundType = ZeroKLobby.Controls.SoundPalette.SoundType.Click;
            this.btnCustomBattles.TabIndex = 17;
            this.btnCustomBattles.Text = "Custom battle";
            this.btnCustomBattles.UseVisualStyleBackColor = false;
            this.btnCustomBattles.Click += new System.EventHandler(this.btnCustomBattles_Click);
            // 
            // btnJoinQueue
            // 
            this.btnJoinQueue.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.btnJoinQueue.BackColor = System.Drawing.Color.Transparent;
            this.btnJoinQueue.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("btnJoinQueue.BackgroundImage")));
            this.btnJoinQueue.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.btnJoinQueue.ButtonStyle = ZeroKLobby.FrameBorderRenderer.StyleType.DarkHive;
            this.btnJoinQueue.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnJoinQueue.FlatAppearance.BorderSize = 0;
            this.btnJoinQueue.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Transparent;
            this.btnJoinQueue.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Transparent;
            this.btnJoinQueue.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnJoinQueue.ForeColor = System.Drawing.Color.White;
            this.btnJoinQueue.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnJoinQueue.Location = new System.Drawing.Point(36, 184);
            this.btnJoinQueue.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btnJoinQueue.Name = "btnJoinQueue";
            this.btnJoinQueue.Size = new System.Drawing.Size(375, 77);
            this.btnJoinQueue.SoundType = ZeroKLobby.Controls.SoundPalette.SoundType.Click;
            this.btnJoinQueue.TabIndex = 16;
            this.btnJoinQueue.Text = "Join queue (recommended)";
            this.btnJoinQueue.UseVisualStyleBackColor = false;
            // 
            // MultiPlayerPage
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.Transparent;
            this.Controls.Add(this.btnSpectate);
            this.Controls.Add(this.btnCustomBattles);
            this.Controls.Add(this.btnJoinQueue);
            this.DoubleBuffered = true;
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.Name = "MultiPlayerPage";
            this.Size = new System.Drawing.Size(1198, 794);
            this.ResumeLayout(false);

        }

        #endregion

        private BitmapButton btnSpectate;
        private BitmapButton btnCustomBattles;
        private BitmapButton btnJoinQueue;
    }
}
