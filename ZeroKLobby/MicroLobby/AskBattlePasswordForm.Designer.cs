namespace ZeroKLobby.MicroLobby
{
    partial class AskBattlePasswordForm
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
					this.lbTitle = new System.Windows.Forms.Label();
					this.tbPassword = new System.Windows.Forms.TextBox();
					this.btnOk = new System.Windows.Forms.Button();
					this.btnCancel = new System.Windows.Forms.Button();
					this.SuspendLayout();
					// 
					// lbTitle
					// 
					this.lbTitle.AutoSize = true;
					this.lbTitle.Location = new System.Drawing.Point(12, 20);
					this.lbTitle.Name = "lbTitle";
					this.lbTitle.Size = new System.Drawing.Size(228, 13);
					this.lbTitle.TabIndex = 0;
					this.lbTitle.Text = "Please enter password for the battle hosted by ";
					// 
					// tbPassword
					// 
					this.tbPassword.Location = new System.Drawing.Point(109, 53);
					this.tbPassword.Name = "tbPassword";
					this.tbPassword.Size = new System.Drawing.Size(113, 20);
					this.tbPassword.TabIndex = 0;
					this.tbPassword.UseSystemPasswordChar = true;
					// 
					// btnOk
					// 
					this.btnOk.DialogResult = System.Windows.Forms.DialogResult.Cancel;
					this.btnOk.Location = new System.Drawing.Point(42, 92);
					this.btnOk.Name = "btnOk";
					this.btnOk.Size = new System.Drawing.Size(75, 23);
					this.btnOk.TabIndex = 1;
					this.btnOk.Text = "OK";
					this.btnOk.UseVisualStyleBackColor = true;
					this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
					// 
					// btnCancel
					// 
					this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
					this.btnCancel.Location = new System.Drawing.Point(203, 92);
					this.btnCancel.Name = "btnCancel";
					this.btnCancel.Size = new System.Drawing.Size(75, 23);
					this.btnCancel.TabIndex = 2;
					this.btnCancel.Text = "Cancel";
					this.btnCancel.UseVisualStyleBackColor = true;
					this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
					// 
					// AskBattlePasswordForm
					// 
					this.AcceptButton = this.btnOk;
					this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
					this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
					this.CancelButton = this.btnCancel;
					this.ClientSize = new System.Drawing.Size(318, 149);
					this.Controls.Add(this.btnCancel);
					this.Controls.Add(this.btnOk);
					this.Controls.Add(this.tbPassword);
					this.Controls.Add(this.lbTitle);
					this.MaximizeBox = false;
					this.MinimizeBox = false;
					this.Name = "AskBattlePasswordForm";
					this.Text = "Battle is passworded";
					this.Load += new System.EventHandler(this.AskBattlePasswordForm_Load);
					this.ResumeLayout(false);
					this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lbTitle;
        private System.Windows.Forms.TextBox tbPassword;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Button btnCancel;
    }
}