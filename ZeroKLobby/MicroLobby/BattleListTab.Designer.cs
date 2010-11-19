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
          this.moreButton = new System.Windows.Forms.Button();
          this.btnQuickJoin = new System.Windows.Forms.Button();
          this.SuspendLayout();
          // 
          // moreButton
          // 
          this.moreButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
          this.moreButton.Location = new System.Drawing.Point(17, 107);
          this.moreButton.Name = "moreButton";
          this.moreButton.Size = new System.Drawing.Size(45, 23);
          this.moreButton.TabIndex = 0;
          this.moreButton.Text = "More";
          this.moreButton.UseVisualStyleBackColor = true;
          // 
          // btnQuickJoin
          // 
          this.btnQuickJoin.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
          this.btnQuickJoin.Location = new System.Drawing.Point(68, 107);
          this.btnQuickJoin.Name = "btnQuickJoin";
          this.btnQuickJoin.Size = new System.Drawing.Size(69, 23);
          this.btnQuickJoin.TabIndex = 1;
          this.btnQuickJoin.Text = "QuickJoin";
          this.btnQuickJoin.UseVisualStyleBackColor = true;
          this.btnQuickJoin.Click += new System.EventHandler(this.button1_Click);
          // 
          // BattleListTab
          // 
          this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
          this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
          this.Controls.Add(this.btnQuickJoin);
          this.Controls.Add(this.moreButton);
          this.Name = "BattleListTab";
          this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button moreButton;
        private System.Windows.Forms.Button btnQuickJoin;
    }
}
