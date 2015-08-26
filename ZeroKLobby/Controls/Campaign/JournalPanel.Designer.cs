namespace ZeroKLobby.Controls.Campaign
{
    partial class JournalPanel
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(JournalPanel));
            this.journalTree = new System.Windows.Forms.TreeView();
            this.closeButton = new ZeroKLobby.BitmapButton();
            this.subpanel = new ZeroKLobby.Controls.Campaign.JournalSubPanel();
            this.SuspendLayout();
            // 
            // journalTree
            // 
            this.journalTree.BackColor = System.Drawing.Color.DimGray;
            this.journalTree.Font = new System.Drawing.Font("Microsoft Sans Serif", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.journalTree.ForeColor = System.Drawing.Color.White;
            this.journalTree.Location = new System.Drawing.Point(4, 21);
            this.journalTree.Name = "journalTree";
            this.journalTree.Size = new System.Drawing.Size(299, 550);
            this.journalTree.TabIndex = 6;
            this.journalTree.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.journalTree_AfterSelect);
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
            this.closeButton.Text = "Close";
            this.closeButton.UseVisualStyleBackColor = false;
            this.closeButton.Click += new System.EventHandler(this.closeButton_Click);
            // 
            // subpanel
            // 
            this.subpanel.AutoSize = true;
            this.subpanel.BackColor = System.Drawing.Color.Transparent;
            this.subpanel.ForeColor = System.Drawing.Color.White;
            this.subpanel.Location = new System.Drawing.Point(310, 21);
            this.subpanel.Name = "subpanel";
            this.subpanel.Size = new System.Drawing.Size(483, 553);
            this.subpanel.TabIndex = 7;
            // 
            // JournalPanel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.Controls.Add(this.subpanel);
            this.Controls.Add(this.journalTree);
            this.Controls.Add(this.closeButton);
            this.ForeColor = System.Drawing.Color.White;
            this.Name = "JournalPanel";
            this.Size = new System.Drawing.Size(800, 603);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private BitmapButton closeButton;
        private System.Windows.Forms.TreeView journalTree;
        private JournalSubPanel subpanel;
    }
}
