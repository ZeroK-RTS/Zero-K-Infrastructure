namespace ZeroKLobby.Controls
{
    partial class MinimapFuncBox
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
            this.minimapSplitContainer1 = new ZeroKLobby.ZkSplitContainer();
            this.layoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.btnGameOptions = new ZeroKLobby.BitmapButton();
            this.btnMapList = new ZeroKLobby.BitmapButton();
            this.btnChangeTeam = new ZeroKLobby.BitmapButton();
            this.btnAddAI = new ZeroKLobby.BitmapButton();
            this.mapPanel = new System.Windows.Forms.Panel();
            this.minimapSplitContainer1.Panel1.SuspendLayout();
            this.minimapSplitContainer1.Panel2.SuspendLayout();
            this.minimapSplitContainer1.SuspendLayout();
            this.layoutPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // minimapSplitContainer1
            // 
            this.minimapSplitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.minimapSplitContainer1.IsSplitterFixed = true;
            this.minimapSplitContainer1.Location = new System.Drawing.Point(0, 0);
            this.minimapSplitContainer1.Margin = new System.Windows.Forms.Padding(0);
            this.minimapSplitContainer1.Name = "minimapSplitContainer1";
            this.minimapSplitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // minimapSplitContainer1.Panel1
            // 
            this.minimapSplitContainer1.Panel1.Controls.Add(this.layoutPanel);
            this.minimapSplitContainer1.Panel1MinSize = 10;
            // 
            // minimapSplitContainer1.Panel2
            // 
            this.minimapSplitContainer1.Panel2.Controls.Add(this.mapPanel);
            this.minimapSplitContainer1.Panel2MinSize = 10;
            this.minimapSplitContainer1.Size = new System.Drawing.Size(198, 172);
            this.minimapSplitContainer1.SplitterDistance = 25;
            this.minimapSplitContainer1.TabIndex = 1;
            // 
            // layoutPanel
            // 
            this.layoutPanel.AutoSize = true;
            this.layoutPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.layoutPanel.ColumnCount = 4;
            this.layoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.layoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.layoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.layoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.layoutPanel.Controls.Add(this.btnGameOptions, 0, 0);
            this.layoutPanel.Controls.Add(this.btnMapList, 0, 0);
            this.layoutPanel.Controls.Add(this.btnChangeTeam, 0, 0);
            this.layoutPanel.Controls.Add(this.btnAddAI, 0, 0);
            this.layoutPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.layoutPanel.Location = new System.Drawing.Point(0, 0);
            this.layoutPanel.MinimumSize = new System.Drawing.Size(24, 23);
            this.layoutPanel.Name = "layoutPanel";
            this.layoutPanel.RowCount = 1;
            this.layoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.layoutPanel.Size = new System.Drawing.Size(198, 25);
            this.layoutPanel.TabIndex = 2;
            // 
            // btnGameOptions
            // 
            this.btnGameOptions.BackColor = System.Drawing.Color.Transparent;
            this.btnGameOptions.BackgroundImage = global::ZeroKLobby.Buttons.panel;
            this.btnGameOptions.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btnGameOptions.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnGameOptions.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnGameOptions.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnGameOptions.ForeColor = System.Drawing.Color.White;
            this.btnGameOptions.Location = new System.Drawing.Point(98, 0);
            this.btnGameOptions.Margin = new System.Windows.Forms.Padding(0);
            this.btnGameOptions.MinimumSize = new System.Drawing.Size(0, 23);
            this.btnGameOptions.Name = "btnGameOptions";
            this.btnGameOptions.Size = new System.Drawing.Size(49, 25);
            this.btnGameOptions.TabIndex = 18;
            this.btnGameOptions.Text = "Options";
            this.btnGameOptions.UseVisualStyleBackColor = true;
            this.btnGameOptions.Click += new System.EventHandler(this.btnGameOptions_Click);
            // 
            // btnMapList
            // 
            this.btnMapList.BackColor = System.Drawing.Color.Transparent;
            this.btnMapList.BackgroundImage = global::ZeroKLobby.Buttons.panel;
            this.btnMapList.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btnMapList.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnMapList.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnMapList.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnMapList.ForeColor = System.Drawing.Color.White;
            this.btnMapList.Location = new System.Drawing.Point(147, 0);
            this.btnMapList.Margin = new System.Windows.Forms.Padding(0);
            this.btnMapList.MinimumSize = new System.Drawing.Size(0, 23);
            this.btnMapList.Name = "btnMapList";
            this.btnMapList.Size = new System.Drawing.Size(51, 25);
            this.btnMapList.TabIndex = 17;
            this.btnMapList.Text = "Maps";
            this.btnMapList.UseVisualStyleBackColor = true;
            this.btnMapList.Click += new System.EventHandler(this.btnMapList_Click);
            // 
            // btnChangeTeam
            // 
            this.btnChangeTeam.BackColor = System.Drawing.Color.Transparent;
            this.btnChangeTeam.BackgroundImage = global::ZeroKLobby.Buttons.panel;
            this.btnChangeTeam.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btnChangeTeam.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnChangeTeam.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnChangeTeam.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnChangeTeam.ForeColor = System.Drawing.Color.White;
            this.btnChangeTeam.Location = new System.Drawing.Point(0, 0);
            this.btnChangeTeam.Margin = new System.Windows.Forms.Padding(0);
            this.btnChangeTeam.MinimumSize = new System.Drawing.Size(0, 23);
            this.btnChangeTeam.Name = "btnChangeTeam";
            this.btnChangeTeam.Size = new System.Drawing.Size(49, 25);
            this.btnChangeTeam.TabIndex = 16;
            this.btnChangeTeam.Text = "Teams";
            this.btnChangeTeam.UseVisualStyleBackColor = false;
            this.btnChangeTeam.Click += new System.EventHandler(this.changeTeamButton_Click);
            // 
            // btnAddAI
            // 
            this.btnAddAI.BackColor = System.Drawing.Color.Transparent;
            this.btnAddAI.BackgroundImage = global::ZeroKLobby.Buttons.panel;
            this.btnAddAI.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btnAddAI.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnAddAI.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnAddAI.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnAddAI.ForeColor = System.Drawing.Color.White;
            this.btnAddAI.Location = new System.Drawing.Point(49, 0);
            this.btnAddAI.Margin = new System.Windows.Forms.Padding(0);
            this.btnAddAI.MinimumSize = new System.Drawing.Size(0, 23);
            this.btnAddAI.Name = "btnAddAI";
            this.btnAddAI.Size = new System.Drawing.Size(49, 25);
            this.btnAddAI.TabIndex = 15;
            this.btnAddAI.Text = "Add AI";
            this.btnAddAI.UseVisualStyleBackColor = true;
            this.btnAddAI.Click += new System.EventHandler(this.addAIButton_Click);
            // 
            // mapPanel
            // 
            this.mapPanel.AutoSize = true;
            this.mapPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.mapPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mapPanel.Location = new System.Drawing.Point(0, 0);
            this.mapPanel.Name = "mapPanel";
            this.mapPanel.Size = new System.Drawing.Size(198, 143);
            this.mapPanel.TabIndex = 2;
            // 
            // MinimapFuncBox
            // 
            this.AutoSize = true;
            this.Dock = System.Windows.Forms.DockStyle.Fill;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.minimapSplitContainer1);
            this.Name = "MinimapFuncBox";
            this.Size = new System.Drawing.Size(198, 172);
            this.minimapSplitContainer1.Panel1.ResumeLayout(false);
            this.minimapSplitContainer1.Panel1.PerformLayout();
            this.minimapSplitContainer1.Panel2.ResumeLayout(false);
            this.minimapSplitContainer1.Panel2.PerformLayout();
            this.minimapSplitContainer1.ResumeLayout(false);
            this.layoutPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        public System.Windows.Forms.Panel mapPanel;
        public ZkSplitContainer minimapSplitContainer1;
        private System.Windows.Forms.TableLayoutPanel layoutPanel;
        private BitmapButton btnGameOptions;
        private BitmapButton btnMapList;
        private BitmapButton btnChangeTeam;
        private BitmapButton btnAddAI;
    }
}
