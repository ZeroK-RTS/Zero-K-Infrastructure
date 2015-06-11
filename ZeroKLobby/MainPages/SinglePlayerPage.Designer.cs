namespace ZeroKLobby.MainPages
{
    partial class SinglePlayerPage
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SinglePlayerPage));
            this.skirmishButton = new ZeroKLobby.BitmapButton();
            this.campaignButton = new ZeroKLobby.BitmapButton();
            this.missonsButton = new ZeroKLobby.BitmapButton();
            this.tutorialButton = new ZeroKLobby.BitmapButton();
            this.SuspendLayout();
            // 
            // skirmishButton
            // 
            this.skirmishButton.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.skirmishButton.BackColor = System.Drawing.Color.Transparent;
            this.skirmishButton.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("skirmishButton.BackgroundImage")));
            this.skirmishButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.skirmishButton.ButtonStyle = ZeroKLobby.FrameBorderRenderer.StyleType.DarkHive;
            this.skirmishButton.Cursor = System.Windows.Forms.Cursors.Hand;
            this.skirmishButton.FlatAppearance.BorderSize = 0;
            this.skirmishButton.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Transparent;
            this.skirmishButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Transparent;
            this.skirmishButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.skirmishButton.ForeColor = System.Drawing.Color.White;
            this.skirmishButton.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.skirmishButton.Location = new System.Drawing.Point(41, 514);
            this.skirmishButton.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.skirmishButton.Name = "skirmishButton";
            this.skirmishButton.Size = new System.Drawing.Size(375, 77);
            this.skirmishButton.SoundType = ZeroKLobby.Controls.SoundPalette.SoundType.Servo;
            this.skirmishButton.TabIndex = 14;
            this.skirmishButton.Text = "Skirmish";
            this.skirmishButton.UseVisualStyleBackColor = false;
            this.skirmishButton.Click += new System.EventHandler(this.skirmishButton_Click);
            // 
            // campaignButton
            // 
            this.campaignButton.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.campaignButton.BackColor = System.Drawing.Color.Transparent;
            this.campaignButton.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("campaignButton.BackgroundImage")));
            this.campaignButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.campaignButton.ButtonStyle = ZeroKLobby.FrameBorderRenderer.StyleType.DarkHive;
            this.campaignButton.Cursor = System.Windows.Forms.Cursors.Hand;
            this.campaignButton.FlatAppearance.BorderSize = 0;
            this.campaignButton.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Transparent;
            this.campaignButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Transparent;
            this.campaignButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.campaignButton.ForeColor = System.Drawing.Color.White;
            this.campaignButton.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.campaignButton.Location = new System.Drawing.Point(41, 390);
            this.campaignButton.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.campaignButton.Name = "campaignButton";
            this.campaignButton.Size = new System.Drawing.Size(375, 77);
            this.campaignButton.SoundType = ZeroKLobby.Controls.SoundPalette.SoundType.Servo;
            this.campaignButton.TabIndex = 13;
            this.campaignButton.Text = "Campaign";
            this.campaignButton.UseVisualStyleBackColor = false;
            this.campaignButton.Click += new System.EventHandler(this.campaignButton_Click);
            // 
            // missonsButton
            // 
            this.missonsButton.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.missonsButton.BackColor = System.Drawing.Color.Transparent;
            this.missonsButton.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("missonsButton.BackgroundImage")));
            this.missonsButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.missonsButton.ButtonStyle = ZeroKLobby.FrameBorderRenderer.StyleType.DarkHive;
            this.missonsButton.Cursor = System.Windows.Forms.Cursors.Hand;
            this.missonsButton.FlatAppearance.BorderSize = 0;
            this.missonsButton.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Transparent;
            this.missonsButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Transparent;
            this.missonsButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.missonsButton.ForeColor = System.Drawing.Color.White;
            this.missonsButton.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.missonsButton.Location = new System.Drawing.Point(41, 261);
            this.missonsButton.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.missonsButton.Name = "missonsButton";
            this.missonsButton.Size = new System.Drawing.Size(375, 77);
            this.missonsButton.SoundType = ZeroKLobby.Controls.SoundPalette.SoundType.Servo;
            this.missonsButton.TabIndex = 12;
            this.missonsButton.Text = "Missions";
            this.missonsButton.UseVisualStyleBackColor = false;
            this.missonsButton.Click += new System.EventHandler(this.missonsButton_Click);
            // 
            // tutorialButton
            // 
            this.tutorialButton.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.tutorialButton.BackColor = System.Drawing.Color.Transparent;
            this.tutorialButton.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("tutorialButton.BackgroundImage")));
            this.tutorialButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.tutorialButton.ButtonStyle = ZeroKLobby.FrameBorderRenderer.StyleType.DarkHive;
            this.tutorialButton.Cursor = System.Windows.Forms.Cursors.Hand;
            this.tutorialButton.FlatAppearance.BorderSize = 0;
            this.tutorialButton.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Transparent;
            this.tutorialButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Transparent;
            this.tutorialButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.tutorialButton.ForeColor = System.Drawing.Color.White;
            this.tutorialButton.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.tutorialButton.Location = new System.Drawing.Point(41, 138);
            this.tutorialButton.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.tutorialButton.Name = "tutorialButton";
            this.tutorialButton.Size = new System.Drawing.Size(375, 77);
            this.tutorialButton.SoundType = ZeroKLobby.Controls.SoundPalette.SoundType.Servo;
            this.tutorialButton.TabIndex = 11;
            this.tutorialButton.Text = "Tutorial";
            this.tutorialButton.UseVisualStyleBackColor = false;
            this.tutorialButton.Click += new System.EventHandler(this.tutorialButton_Click);
            // 
            // SinglePlayerPage
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.Transparent;
            this.Controls.Add(this.skirmishButton);
            this.Controls.Add(this.campaignButton);
            this.Controls.Add(this.missonsButton);
            this.Controls.Add(this.tutorialButton);
            this.DoubleBuffered = true;
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.Name = "SinglePlayerPage";
            this.Size = new System.Drawing.Size(1296, 798);
            this.ResumeLayout(false);

        }

        #endregion

        private BitmapButton tutorialButton;
        private BitmapButton missonsButton;
        private BitmapButton campaignButton;
        private BitmapButton skirmishButton;

    }
}
