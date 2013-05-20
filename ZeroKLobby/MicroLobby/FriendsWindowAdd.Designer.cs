namespace ZeroKLobby.MicroLobby
{
    partial class FriendsWindowAdd
    {
        /// <summary>
        /// Erforderliche Designervariable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Verwendete Ressourcen bereinigen.
        /// </summary>
        /// <param name="disposing">True, wenn verwaltete Ressourcen gelöscht werden sollen; andernfalls False.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Vom Windows Form-Designer generierter Code

        /// <summary>
        /// Erforderliche Methode für die Designerunterstützung.
        /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
        /// </summary>
        private void InitializeComponent()
        {
            this.labelAddFriend = new System.Windows.Forms.Label();
            this.buttonCancel = new ZeroKLobby.BitmapButton();
            this.buttonOK = new ZeroKLobby.BitmapButton();
            this.textBoxAddFriend = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // labelAddFriend
            // 
            this.labelAddFriend.AutoSize = true;
            this.labelAddFriend.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.labelAddFriend.ForeColor = System.Drawing.SystemColors.ButtonHighlight;
            this.labelAddFriend.Location = new System.Drawing.Point(12, 9);
            this.labelAddFriend.Name = "labelAddFriend";
            this.labelAddFriend.Size = new System.Drawing.Size(67, 13);
            this.labelAddFriend.TabIndex = 0;
            this.labelAddFriend.Text = "Add a friend:";
            // 
            // buttonCancel
            // 
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonCancel.FlatAppearance.BorderColor = System.Drawing.Color.DeepSkyBlue;
            this.buttonCancel.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(5)))), ((int)(((byte)(20)))), ((int)(((byte)(50)))));
            this.buttonCancel.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(5)))), ((int)(((byte)(20)))), ((int)(((byte)(50)))));
            this.buttonCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonCancel.ForeColor = System.Drawing.SystemColors.ButtonHighlight;
            this.buttonCancel.Location = new System.Drawing.Point(12, 55);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(75, 23);
            this.buttonCancel.TabIndex = 1;
            this.buttonCancel.Text = "Cancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            // 
            // buttonOK
            // 
            this.buttonOK.FlatAppearance.BorderColor = System.Drawing.Color.DeepSkyBlue;
            this.buttonOK.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(5)))), ((int)(((byte)(20)))), ((int)(((byte)(50)))));
            this.buttonOK.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(5)))), ((int)(((byte)(20)))), ((int)(((byte)(50)))));
            this.buttonOK.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonOK.ForeColor = System.Drawing.SystemColors.ButtonHighlight;
            this.buttonOK.Location = new System.Drawing.Point(97, 55);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.Size = new System.Drawing.Size(75, 23);
            this.buttonOK.TabIndex = 1;
            this.buttonOK.Text = "OK";
            this.buttonOK.UseVisualStyleBackColor = true;
            this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
            // 
            // textBoxAddFriend
            // 
            this.textBoxAddFriend.Location = new System.Drawing.Point(12, 29);
            this.textBoxAddFriend.Name = "textBoxAddFriend";
            this.textBoxAddFriend.Size = new System.Drawing.Size(160, 20);
            this.textBoxAddFriend.TabIndex = 2;
            // 
            // FriendsWindowAdd
            // 
            this.AcceptButton = this.buttonOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.CancelButton = this.buttonCancel;
            this.ClientSize = new System.Drawing.Size(184, 90);
            this.ControlBox = false;
            this.Controls.Add(this.textBoxAddFriend);
            this.Controls.Add(this.buttonOK);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.labelAddFriend);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "FriendsWindowAdd";
            this.Text = "Add your friend";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label labelAddFriend;
        private System.Windows.Forms.Button buttonCancel;
        private System.Windows.Forms.Button buttonOK;
        private System.Windows.Forms.TextBox textBoxAddFriend;
    }
}