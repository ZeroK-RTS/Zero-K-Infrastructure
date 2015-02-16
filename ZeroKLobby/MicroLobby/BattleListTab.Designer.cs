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
            this.hidePasswordedBox = new System.Windows.Forms.CheckBox();
            this.showOfficialBox = new System.Windows.Forms.CheckBox();
            this.hideEmptyBox = new System.Windows.Forms.CheckBox();
            this.hideFullBox = new System.Windows.Forms.CheckBox();
            this.searchLabel = new System.Windows.Forms.Label();
            this.searchBox = new System.Windows.Forms.TextBox();
            this.battlePanel = new System.Windows.Forms.Panel();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.Color.DimGray;
            this.panel1.Controls.Add(this.hidePasswordedBox);
            this.panel1.Controls.Add(this.showOfficialBox);
            this.panel1.Controls.Add(this.hideEmptyBox);
            this.panel1.Controls.Add(this.hideFullBox);
            this.panel1.Controls.Add(this.searchLabel);
            this.panel1.Controls.Add(this.searchBox);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.ForeColor = System.Drawing.Color.White;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(1096, 48);
            this.panel1.TabIndex = 0;
            // 
            // hidePasswordedBox
            // 
            this.hidePasswordedBox.AutoSize = true;
            this.hidePasswordedBox.Location = new System.Drawing.Point(584, 11);
            this.hidePasswordedBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.hidePasswordedBox.Name = "hidePasswordedBox";
            this.hidePasswordedBox.Size = new System.Drawing.Size(159, 24);
            this.hidePasswordedBox.TabIndex = 7;
            this.hidePasswordedBox.Text = "Hide Passworded";
            this.hidePasswordedBox.UseVisualStyleBackColor = true;
            this.hidePasswordedBox.CheckedChanged += new System.EventHandler(this.hidePasswordedBox_CheckedChanged);
            // 
            // showOfficialBox
            // 
            this.showOfficialBox.AutoSize = true;
            this.showOfficialBox.Location = new System.Drawing.Point(756, 11);
            this.showOfficialBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.showOfficialBox.Name = "showOfficialBox";
            this.showOfficialBox.Size = new System.Drawing.Size(118, 24);
            this.showOfficialBox.TabIndex = 6;
            this.showOfficialBox.Text = "Official Only";
            this.showOfficialBox.UseVisualStyleBackColor = true;
            this.showOfficialBox.CheckedChanged += new System.EventHandler(this.showOfficialButton_CheckedChanged);
            // 
            // hideEmptyBox
            // 
            this.hideEmptyBox.AutoSize = true;
            this.hideEmptyBox.Location = new System.Drawing.Point(454, 11);
            this.hideEmptyBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.hideEmptyBox.Name = "hideEmptyBox";
            this.hideEmptyBox.Size = new System.Drawing.Size(117, 24);
            this.hideEmptyBox.TabIndex = 5;
            this.hideEmptyBox.Text = "Hide Empty";
            this.hideEmptyBox.UseVisualStyleBackColor = true;
            this.hideEmptyBox.CheckedChanged += new System.EventHandler(this.showEmptyBox_CheckedChanged);
            // 
            // hideFullBox
            // 
            this.hideFullBox.AutoSize = true;
            this.hideFullBox.Location = new System.Drawing.Point(345, 11);
            this.hideFullBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.hideFullBox.Name = "hideFullBox";
            this.hideFullBox.Size = new System.Drawing.Size(97, 24);
            this.hideFullBox.TabIndex = 4;
            this.hideFullBox.Text = "Hide Full";
            this.hideFullBox.UseVisualStyleBackColor = true;
            this.hideFullBox.CheckedChanged += new System.EventHandler(this.showFullBox_CheckedChanged);
            // 
            // searchLabel
            // 
            this.searchLabel.AutoSize = true;
            this.searchLabel.Location = new System.Drawing.Point(4, 11);
            this.searchLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.searchLabel.Name = "searchLabel";
            this.searchLabel.Size = new System.Drawing.Size(60, 20);
            this.searchLabel.TabIndex = 3;
            this.searchLabel.Text = "Search";
            // 
            // searchBox
            // 
            this.searchBox.Location = new System.Drawing.Point(69, 8);
            this.searchBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.searchBox.Name = "searchBox";
            this.searchBox.Size = new System.Drawing.Size(265, 26);
            this.searchBox.TabIndex = 2;
            this.searchBox.TextChanged += new System.EventHandler(this.searchBox_TextChanged);
            // 
            // battlePanel
            // 
            this.battlePanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.battlePanel.Location = new System.Drawing.Point(0, 48);
            this.battlePanel.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.battlePanel.Name = "battlePanel";
            this.battlePanel.Size = new System.Drawing.Size(1096, 664);
            this.battlePanel.TabIndex = 1;
            // 
            // BattleListTab
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.Controls.Add(this.battlePanel);
            this.Controls.Add(this.panel1);
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.Name = "BattleListTab";
            this.Size = new System.Drawing.Size(1096, 712);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.CheckBox showOfficialBox;
        private System.Windows.Forms.CheckBox hideEmptyBox;
        private System.Windows.Forms.CheckBox hideFullBox;
        private System.Windows.Forms.Label searchLabel;
        private System.Windows.Forms.TextBox searchBox;
        private System.Windows.Forms.Panel battlePanel;
        private System.Windows.Forms.CheckBox hidePasswordedBox;

    }
}
