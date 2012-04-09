namespace ZeroKLobby.Notifications
{
    partial class JugglerBar
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
            this.lbInfo = new System.Windows.Forms.Label();
            this.cbMatchMake = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // lbInfo
            // 
            this.lbInfo.AutoSize = true;
            this.lbInfo.Location = new System.Drawing.Point(13, 10);
            this.lbInfo.Name = "lbInfo";
            this.lbInfo.Size = new System.Drawing.Size(0, 13);
            this.lbInfo.TabIndex = 0;
            // 
            // cbMatchMake
            // 
            this.cbMatchMake.AutoSize = true;
            this.cbMatchMake.Location = new System.Drawing.Point(16, 50);
            this.cbMatchMake.Name = "cbMatchMake";
            this.cbMatchMake.Size = new System.Drawing.Size(85, 17);
            this.cbMatchMake.TabIndex = 2;
            this.cbMatchMake.Text = "FIND GAME";
            this.cbMatchMake.UseVisualStyleBackColor = true;
            // 
            // JugglerBar
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.cbMatchMake);
            this.Controls.Add(this.lbInfo);
            this.Name = "JugglerBar";
            this.Size = new System.Drawing.Size(429, 85);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lbInfo;
        private System.Windows.Forms.CheckBox cbMatchMake;
    }
}
