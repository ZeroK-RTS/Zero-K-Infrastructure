namespace ZeroKLobby.Controls.Campaign
{
    partial class JournalPopupPanel
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(JournalPopupPanel));
            this.closeButton = new ZeroKLobby.BitmapButton();
            this.subpanel = new ZeroKLobby.Controls.Campaign.JournalSubPanel();
            this.playButton = new ZeroKLobby.BitmapButton();
            this.SuspendLayout();
            // 
            // closeButton
            // 
            this.closeButton.BackColor = System.Drawing.Color.Transparent;
            this.closeButton.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("closeButton.BackgroundImage")));
            this.closeButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.closeButton.ButtonStyle = ZeroKLobby.FrameBorderRenderer.StyleType.DarkHive;
            this.closeButton.Cursor = System.Windows.Forms.Cursors.Hand;
            this.closeButton.FlatAppearance.BorderSize = 0;
            this.closeButton.FlatAppearance.CheckedBackColor = System.Drawing.Color.Transparent;
            this.closeButton.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Transparent;
            this.closeButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Transparent;
            this.closeButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.closeButton.ForeColor = System.Drawing.Color.White;
            this.closeButton.Location = new System.Drawing.Point(3, 577);
            this.closeButton.Name = "closeButton";
            this.closeButton.Size = new System.Drawing.Size(75, 23);
            this.closeButton.SoundType = ZeroKLobby.Controls.SoundPalette.SoundType.Click;
            this.closeButton.TabIndex = 3;
            this.closeButton.Text = "Back";
            this.closeButton.UseVisualStyleBackColor = false;
            this.closeButton.Click += new System.EventHandler(this.closeButton_Click);
            // 
            // subpanel
            // 
            this.subpanel.AutoSize = true;
            this.subpanel.BackColor = System.Drawing.Color.Transparent;
            this.subpanel.ForeColor = System.Drawing.Color.White;
            this.subpanel.Location = new System.Drawing.Point(3, 18);
            this.subpanel.Name = "subpanel";
            this.subpanel.Size = new System.Drawing.Size(483, 553);
            this.subpanel.TabIndex = 7;
            // 
            // playButton
            // 
            this.playButton.BackColor = System.Drawing.Color.Transparent;
            this.playButton.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("playButton.BackgroundImage")));
            this.playButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.playButton.ButtonStyle = ZeroKLobby.FrameBorderRenderer.StyleType.DarkHive;
            this.playButton.Cursor = System.Windows.Forms.Cursors.Hand;
            this.playButton.FlatAppearance.BorderSize = 0;
            this.playButton.FlatAppearance.CheckedBackColor = System.Drawing.Color.Transparent;
            this.playButton.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Transparent;
            this.playButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Transparent;
            this.playButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.playButton.ForeColor = System.Drawing.Color.White;
            this.playButton.Location = new System.Drawing.Point(411, 577);
            this.playButton.Name = "playButton";
            this.playButton.Size = new System.Drawing.Size(75, 23);
            this.playButton.SoundType = ZeroKLobby.Controls.SoundPalette.SoundType.Click;
            this.playButton.TabIndex = 8;
            this.playButton.Text = "Play";
            this.playButton.UseVisualStyleBackColor = false;
            // 
            // JournalPopupPanel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.Controls.Add(this.playButton);
            this.Controls.Add(this.subpanel);
            this.Controls.Add(this.closeButton);
            this.ForeColor = System.Drawing.Color.White;
            this.Name = "JournalPopupPanel";
            this.Size = new System.Drawing.Size(489, 603);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private BitmapButton closeButton;
        private JournalSubPanel subpanel;
        private BitmapButton playButton;
    }
}
