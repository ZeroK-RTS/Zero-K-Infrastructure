namespace ZeroKLobby.MicroLobby
{
    partial class BattleListTab
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
            this.panel1 = new System.Windows.Forms.Panel();
            this.showOfficialBox = new System.Windows.Forms.CheckBox();
            this.showEmptyBox = new System.Windows.Forms.CheckBox();
            this.showFullBox = new System.Windows.Forms.CheckBox();
            this.searchLabel = new System.Windows.Forms.Label();
            this.searchBox = new System.Windows.Forms.TextBox();
            this.newBattleButton = new System.Windows.Forms.Button();
            this.quickmatchButton = new System.Windows.Forms.Button();
            this.battlePanel = new System.Windows.Forms.Panel();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.showOfficialBox);
            this.panel1.Controls.Add(this.showEmptyBox);
            this.panel1.Controls.Add(this.showFullBox);
            this.panel1.Controls.Add(this.searchLabel);
            this.panel1.Controls.Add(this.searchBox);
            this.panel1.Controls.Add(this.newBattleButton);
            this.panel1.Controls.Add(this.quickmatchButton);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(731, 31);
            this.panel1.TabIndex = 0;
            // 
            // showOfficialBox
            // 
            this.showOfficialBox.AutoSize = true;
            this.showOfficialBox.Location = new System.Drawing.Point(576, 5);
            this.showOfficialBox.Name = "showOfficialBox";
            this.showOfficialBox.Size = new System.Drawing.Size(82, 17);
            this.showOfficialBox.TabIndex = 6;
            this.showOfficialBox.Text = "Official Only";
            this.showOfficialBox.UseVisualStyleBackColor = true;
            this.showOfficialBox.CheckedChanged += new System.EventHandler(this.showOfficialButton_CheckedChanged);
            // 
            // showEmptyBox
            // 
            this.showEmptyBox.AutoSize = true;
            this.showEmptyBox.Location = new System.Drawing.Point(485, 5);
            this.showEmptyBox.Name = "showEmptyBox";
            this.showEmptyBox.Size = new System.Drawing.Size(85, 17);
            this.showEmptyBox.TabIndex = 5;
            this.showEmptyBox.Text = "Show Empty";
            this.showEmptyBox.UseVisualStyleBackColor = true;
            this.showEmptyBox.CheckedChanged += new System.EventHandler(this.showEmptyBox_CheckedChanged);
            // 
            // showFullBox
            // 
            this.showFullBox.AutoSize = true;
            this.showFullBox.Location = new System.Drawing.Point(392, 5);
            this.showFullBox.Name = "showFullBox";
            this.showFullBox.Size = new System.Drawing.Size(72, 17);
            this.showFullBox.TabIndex = 4;
            this.showFullBox.Text = "Show Full";
            this.showFullBox.UseVisualStyleBackColor = true;
            this.showFullBox.CheckedChanged += new System.EventHandler(this.showFullBox_CheckedChanged);
            // 
            // searchLabel
            // 
            this.searchLabel.AutoSize = true;
            this.searchLabel.Location = new System.Drawing.Point(165, 7);
            this.searchLabel.Name = "searchLabel";
            this.searchLabel.Size = new System.Drawing.Size(41, 13);
            this.searchLabel.TabIndex = 3;
            this.searchLabel.Text = "Search";
            // 
            // searchBox
            // 
            this.searchBox.Location = new System.Drawing.Point(208, 5);
            this.searchBox.Name = "searchBox";
            this.searchBox.Size = new System.Drawing.Size(178, 20);
            this.searchBox.TabIndex = 2;
            this.searchBox.TextChanged += new System.EventHandler(this.searchBox_TextChanged);
            // 
            // newBattleButton
            // 
            this.newBattleButton.Location = new System.Drawing.Point(84, 2);
            this.newBattleButton.Name = "newBattleButton";
            this.newBattleButton.Size = new System.Drawing.Size(75, 23);
            this.newBattleButton.TabIndex = 1;
            this.newBattleButton.Text = "Open New";
            this.newBattleButton.UseVisualStyleBackColor = true;
            this.newBattleButton.Click += new System.EventHandler(this.newBattleButton_Click);
            // 
            // quickmatchButton
            // 
            this.quickmatchButton.Location = new System.Drawing.Point(3, 2);
            this.quickmatchButton.Name = "quickmatchButton";
            this.quickmatchButton.Size = new System.Drawing.Size(75, 23);
            this.quickmatchButton.TabIndex = 0;
            this.quickmatchButton.Text = "Quickmatch";
            this.quickmatchButton.UseVisualStyleBackColor = true;
            this.quickmatchButton.Click += new System.EventHandler(this.quickmatchButton_Click);
            // 
            // battlePanel
            // 
            this.battlePanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.battlePanel.Location = new System.Drawing.Point(0, 31);
            this.battlePanel.Name = "battlePanel";
            this.battlePanel.Size = new System.Drawing.Size(731, 432);
            this.battlePanel.TabIndex = 1;
            // 
            // BattleListTab
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.battlePanel);
            this.Controls.Add(this.panel1);
            this.Name = "BattleListTab";
            this.Size = new System.Drawing.Size(731, 463);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.CheckBox showOfficialBox;
        private System.Windows.Forms.CheckBox showEmptyBox;
        private System.Windows.Forms.CheckBox showFullBox;
        private System.Windows.Forms.Label searchLabel;
        private System.Windows.Forms.TextBox searchBox;
        private System.Windows.Forms.Button newBattleButton;
        private System.Windows.Forms.Button quickmatchButton;
        private System.Windows.Forms.Panel battlePanel;

    }
}
