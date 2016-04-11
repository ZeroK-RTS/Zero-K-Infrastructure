using ZeroKLobby.Controls;

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
            this.searchLabel = new System.Windows.Forms.Label();
            this.searchBox = new ZeroKLobby.Controls.ZklTextBox();
            this.battlePanel = new System.Windows.Forms.Panel();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.Color.Transparent;
            this.panel1.Controls.Add(this.searchLabel);
            this.panel1.Controls.Add(this.searchBox);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.ForeColor = System.Drawing.Color.White;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(731, 31);
            this.panel1.TabIndex = 0;
            // 
            // searchLabel
            // 
            this.searchLabel.AutoSize = true;
            this.searchLabel.Location = new System.Drawing.Point(3, 7);
            this.searchLabel.Name = "searchLabel";
            this.searchLabel.Size = new System.Drawing.Size(59, 18);
            this.searchLabel.TabIndex = 3;
            this.searchLabel.Text = "Search:";
            // 
            // searchBox
            // 
            this.searchBox.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(30)))), ((int)(((byte)(40)))));
            this.searchBox.Location = new System.Drawing.Point(68, 4);
            this.searchBox.Name = "searchBox";
            this.searchBox.Size = new System.Drawing.Size(178, 24);
            this.searchBox.TabIndex = 2;
            this.searchBox.TextChanged += new System.EventHandler(this.searchBox_TextChanged);
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
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 18F);
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
        private System.Windows.Forms.Label searchLabel;
        private ZklTextBox searchBox;
        private System.Windows.Forms.Panel battlePanel;

    }
}
