namespace ZeroKLobby.Controls.Campaign
{
    partial class PlanetInfoPanel
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PlanetInfoPanel));
            this.planetPictureBox = new System.Windows.Forms.PictureBox();
            this.closeButton = new ZeroKLobby.BitmapButton();
            this.blurbWindow = new ZeroKLobby.MicroLobby.TextWindow();
            ((System.ComponentModel.ISupportInitialize)(this.planetPictureBox)).BeginInit();
            this.SuspendLayout();
            // 
            // planetPictureBox
            // 
            this.planetPictureBox.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.planetPictureBox.Location = new System.Drawing.Point(18, 14);
            this.planetPictureBox.Name = "planetPictureBox";
            this.planetPictureBox.Size = new System.Drawing.Size(64, 64);
            this.planetPictureBox.TabIndex = 0;
            this.planetPictureBox.TabStop = false;
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
            this.closeButton.Location = new System.Drawing.Point(402, 138);
            this.closeButton.Name = "closeButton";
            this.closeButton.Size = new System.Drawing.Size(75, 23);
            this.closeButton.SoundType = ZeroKLobby.Controls.SoundPalette.SoundType.Click;
            this.closeButton.TabIndex = 3;
            this.closeButton.Text = "Close";
            this.closeButton.UseVisualStyleBackColor = false;
            this.closeButton.Click += new System.EventHandler(this.closeButton_Click);
            // 
            // blurbWindow
            // 
            this.blurbWindow.BackColor = System.Drawing.Color.Black;
            this.blurbWindow.ChatBackgroundColor = 0;
            this.blurbWindow.DefaultTooltip = null;
            this.blurbWindow.ForeColor = System.Drawing.Color.White;
            this.blurbWindow.HideScroll = false;
            this.blurbWindow.IRCForeColor = 0;
            this.blurbWindow.Location = new System.Drawing.Point(100, 51);
            this.blurbWindow.Name = "blurbWindow";
            this.blurbWindow.NoColorMode = false;
            this.blurbWindow.ShowUnreadLine = true;
            this.blurbWindow.SingleLine = false;
            this.blurbWindow.Size = new System.Drawing.Size(341, 81);
            this.blurbWindow.TabIndex = 4;
            // 
            // PlanetInfoPanel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.blurbWindow);
            this.Controls.Add(this.closeButton);
            this.Controls.Add(this.planetPictureBox);
            this.Name = "PlanetInfoPanel";
            this.Size = new System.Drawing.Size(480, 160);
            ((System.ComponentModel.ISupportInitialize)(this.planetPictureBox)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.PictureBox planetPictureBox;
        private BitmapButton closeButton;
        private MicroLobby.TextWindow blurbWindow;
    }
}
