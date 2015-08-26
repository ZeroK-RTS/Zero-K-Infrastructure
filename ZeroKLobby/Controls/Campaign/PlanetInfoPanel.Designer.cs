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
            this.planetImageBox = new System.Windows.Forms.PictureBox();
            this.missionFlowLayout = new System.Windows.Forms.FlowLayoutPanel();
            this.planetNameBox = new ZeroKLobby.Controls.TransparentTextBox();
            this.planetBlurbBox = new ZeroKLobby.Controls.TransparentTextBox();
            this.closeButton = new ZeroKLobby.BitmapButton();
            ((System.ComponentModel.ISupportInitialize)(this.planetImageBox)).BeginInit();
            this.SuspendLayout();
            // 
            // planetImageBox
            // 
            this.planetImageBox.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.planetImageBox.Location = new System.Drawing.Point(18, 14);
            this.planetImageBox.Name = "planetImageBox";
            this.planetImageBox.Size = new System.Drawing.Size(64, 64);
            this.planetImageBox.TabIndex = 0;
            this.planetImageBox.TabStop = false;
            // 
            // missionFlowLayout
            // 
            this.missionFlowLayout.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.missionFlowLayout.AutoScroll = true;
            this.missionFlowLayout.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.missionFlowLayout.Location = new System.Drawing.Point(100, 139);
            this.missionFlowLayout.Name = "missionFlowLayout";
            this.missionFlowLayout.Size = new System.Drawing.Size(341, 39);
            this.missionFlowLayout.TabIndex = 6;
            // 
            // planetNameBox
            // 
            this.planetNameBox.BackColor = System.Drawing.Color.DimGray;
            this.planetNameBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.planetNameBox.ForeColor = System.Drawing.Color.White;
            this.planetNameBox.Location = new System.Drawing.Point(100, 14);
            this.planetNameBox.Name = "planetNameBox";
            this.planetNameBox.Size = new System.Drawing.Size(341, 31);
            this.planetNameBox.TabIndex = 5;
            // 
            // planetBlurbBox
            // 
            this.planetBlurbBox.BackColor = System.Drawing.Color.DimGray;
            this.planetBlurbBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.planetBlurbBox.ForeColor = System.Drawing.Color.White;
            this.planetBlurbBox.Location = new System.Drawing.Point(100, 51);
            this.planetBlurbBox.Multiline = true;
            this.planetBlurbBox.Name = "planetBlurbBox";
            this.planetBlurbBox.Size = new System.Drawing.Size(341, 81);
            this.planetBlurbBox.TabIndex = 4;
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
            this.closeButton.Location = new System.Drawing.Point(402, 184);
            this.closeButton.Name = "closeButton";
            this.closeButton.Size = new System.Drawing.Size(75, 23);
            this.closeButton.SoundType = ZeroKLobby.Controls.SoundPalette.SoundType.Click;
            this.closeButton.TabIndex = 3;
            this.closeButton.Text = "Close";
            this.closeButton.UseVisualStyleBackColor = false;
            this.closeButton.Click += new System.EventHandler(this.closeButton_Click);
            // 
            // PlanetInfoPanel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.Controls.Add(this.missionFlowLayout);
            this.Controls.Add(this.planetNameBox);
            this.Controls.Add(this.planetBlurbBox);
            this.Controls.Add(this.closeButton);
            this.Controls.Add(this.planetImageBox);
            this.ForeColor = System.Drawing.Color.White;
            this.Name = "PlanetInfoPanel";
            this.Size = new System.Drawing.Size(480, 210);
            ((System.ComponentModel.ISupportInitialize)(this.planetImageBox)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox planetImageBox;
        private BitmapButton closeButton;
        private TransparentTextBox planetBlurbBox;
        private TransparentTextBox planetNameBox;
        private System.Windows.Forms.FlowLayoutPanel missionFlowLayout;
    }
}
