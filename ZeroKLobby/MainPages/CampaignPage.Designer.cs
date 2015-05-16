namespace ZeroKLobby.MicroLobby.Campaign
{
    partial class CampaignPage
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CampaignPage));
            this.loadButton = new ZeroKLobby.BitmapButton();
            this.saveButton = new ZeroKLobby.BitmapButton();
            this.commButton = new ZeroKLobby.BitmapButton();
            this.journalButton = new ZeroKLobby.BitmapButton();
            this.galControl = new ZeroKLobby.Campaign.GalaxyControl();
            this.SuspendLayout();
            // 
            // loadButton
            // 
            this.loadButton.BackColor = System.Drawing.Color.Transparent;
            this.loadButton.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("loadButton.BackgroundImage")));
            this.loadButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.loadButton.ButtonStyle = ZeroKLobby.FrameBorderRenderer.StyleType.DarkHive;
            this.loadButton.Cursor = System.Windows.Forms.Cursors.Hand;
            this.loadButton.FlatAppearance.BorderSize = 0;
            this.loadButton.FlatAppearance.CheckedBackColor = System.Drawing.Color.Transparent;
            this.loadButton.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Transparent;
            this.loadButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Transparent;
            this.loadButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.loadButton.ForeColor = System.Drawing.Color.White;
            this.loadButton.Location = new System.Drawing.Point(0, 704);
            this.loadButton.Name = "loadButton";
            this.loadButton.Size = new System.Drawing.Size(240, 64);
            this.loadButton.SoundType = ZeroKLobby.Controls.SoundPalette.SoundType.Click;
            this.loadButton.TabIndex = 4;
            this.loadButton.Text = "Load";
            this.loadButton.UseVisualStyleBackColor = false;
            this.loadButton.Click += loadButton_Click;
            // 
            // saveButton
            // 
            this.saveButton.BackColor = System.Drawing.Color.Transparent;
            this.saveButton.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("saveButton.BackgroundImage")));
            this.saveButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.saveButton.ButtonStyle = ZeroKLobby.FrameBorderRenderer.StyleType.DarkHive;
            this.saveButton.Cursor = System.Windows.Forms.Cursors.Hand;
            this.saveButton.FlatAppearance.BorderSize = 0;
            this.saveButton.FlatAppearance.CheckedBackColor = System.Drawing.Color.Transparent;
            this.saveButton.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Transparent;
            this.saveButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Transparent;
            this.saveButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.saveButton.ForeColor = System.Drawing.Color.White;
            this.saveButton.Location = new System.Drawing.Point(0, 634);
            this.saveButton.Name = "saveButton";
            this.saveButton.Size = new System.Drawing.Size(240, 64);
            this.saveButton.SoundType = ZeroKLobby.Controls.SoundPalette.SoundType.Click;
            this.saveButton.TabIndex = 3;
            this.saveButton.Text = "Save";
            this.saveButton.UseVisualStyleBackColor = false;
            this.saveButton.Click += saveButton_Click;
            // 
            // commButton
            // 
            this.commButton.BackColor = System.Drawing.Color.Transparent;
            this.commButton.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("commButton.BackgroundImage")));
            this.commButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.commButton.ButtonStyle = ZeroKLobby.FrameBorderRenderer.StyleType.DarkHive;
            this.commButton.Cursor = System.Windows.Forms.Cursors.Hand;
            this.commButton.FlatAppearance.BorderSize = 0;
            this.commButton.FlatAppearance.CheckedBackColor = System.Drawing.Color.Transparent;
            this.commButton.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Transparent;
            this.commButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Transparent;
            this.commButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.commButton.ForeColor = System.Drawing.Color.White;
            this.commButton.Location = new System.Drawing.Point(0, 73);
            this.commButton.Name = "commButton";
            this.commButton.Size = new System.Drawing.Size(240, 64);
            this.commButton.SoundType = ZeroKLobby.Controls.SoundPalette.SoundType.Click;
            this.commButton.TabIndex = 2;
            this.commButton.Text = "Commander";
            this.commButton.UseVisualStyleBackColor = false;
            // 
            // journalButton
            // 
            this.journalButton.BackColor = System.Drawing.Color.Transparent;
            this.journalButton.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("journalButton.BackgroundImage")));
            this.journalButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.journalButton.ButtonStyle = ZeroKLobby.FrameBorderRenderer.StyleType.DarkHive;
            this.journalButton.Cursor = System.Windows.Forms.Cursors.Hand;
            this.journalButton.FlatAppearance.BorderSize = 0;
            this.journalButton.FlatAppearance.CheckedBackColor = System.Drawing.Color.Transparent;
            this.journalButton.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Transparent;
            this.journalButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Transparent;
            this.journalButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.journalButton.ForeColor = System.Drawing.Color.White;
            this.journalButton.Location = new System.Drawing.Point(0, 3);
            this.journalButton.Name = "journalButton";
            this.journalButton.Size = new System.Drawing.Size(240, 64);
            this.journalButton.SoundType = ZeroKLobby.Controls.SoundPalette.SoundType.Click;
            this.journalButton.TabIndex = 1;
            this.journalButton.Text = "Journals";
            this.journalButton.UseVisualStyleBackColor = false;
            // 
            // galControl
            // 
            this.galControl.BackColor = System.Drawing.Color.Black;
            this.galControl.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.galControl.Location = new System.Drawing.Point(256, 0);
            this.galControl.Name = "galControl";
            this.galControl.Size = new System.Drawing.Size(1024, 768);
            this.galControl.TabIndex = 0;
            // 
            // CampaignPage
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.Transparent;
            this.Controls.Add(this.loadButton);
            this.Controls.Add(this.saveButton);
            this.Controls.Add(this.commButton);
            this.Controls.Add(this.journalButton);
            this.Controls.Add(this.galControl);
            this.Name = "CampaignPage";
            this.Size = new System.Drawing.Size(1296, 798);
            this.ResumeLayout(false);

        }

        

        #endregion

        private ZeroKLobby.Campaign.GalaxyControl galControl;
        private BitmapButton journalButton;
        private BitmapButton commButton;
        private BitmapButton saveButton;
        private BitmapButton loadButton;


    }
}
