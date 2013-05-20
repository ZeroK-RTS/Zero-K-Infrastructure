namespace ZeroKLobby.MicroLobby
{
    partial class FriendsWindow
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FriendsWindow));
            this.listBox1 = new System.Windows.Forms.ListBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.buttonAddFriend = new ZeroKLobby.BitmapButton();
            this.labelFriends = new System.Windows.Forms.Label();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // listBox1
            // 
            this.listBox1.BackColor = System.Drawing.SystemColors.Window;
            this.listBox1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.listBox1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.listBox1.FormattingEnabled = true;
            this.listBox1.Location = new System.Drawing.Point(12, 13);
            this.listBox1.Name = "listBox1";
            this.listBox1.Size = new System.Drawing.Size(240, 236);
            this.listBox1.TabIndex = 0;
            this.listBox1.SelectedIndexChanged += new System.EventHandler(this.listBox1_SelectedIndexChanged);
            this.listBox1.DoubleClick += new System.EventHandler(this.listBox1_DoubleClick);
            this.listBox1.MouseDown += new System.Windows.Forms.MouseEventHandler(this.listBox1_MouseDown);
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.buttonAddFriend);
            this.panel1.Controls.Add(this.labelFriends);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel1.Location = new System.Drawing.Point(0, 269);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(264, 45);
            this.panel1.TabIndex = 1;
            // 
            // buttonAddFriend
            // 
            this.buttonAddFriend.BackColor = System.Drawing.SystemColors.MenuText;
            this.buttonAddFriend.FlatAppearance.BorderColor = System.Drawing.Color.DeepSkyBlue;
            this.buttonAddFriend.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(5)))), ((int)(((byte)(20)))), ((int)(((byte)(50)))));
            this.buttonAddFriend.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(5)))), ((int)(((byte)(20)))), ((int)(((byte)(50)))));
            this.buttonAddFriend.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonAddFriend.ForeColor = System.Drawing.SystemColors.ButtonHighlight;
            this.buttonAddFriend.Location = new System.Drawing.Point(13, 10);
            this.buttonAddFriend.Name = "buttonAddFriend";
            this.buttonAddFriend.Size = new System.Drawing.Size(75, 23);
            this.buttonAddFriend.TabIndex = 1;
            this.buttonAddFriend.Text = "Add friend";
            this.buttonAddFriend.UseVisualStyleBackColor = false;
            this.buttonAddFriend.Click += new System.EventHandler(this.buttonAddFriend_Click);
            // 
            // labelFriends
            // 
            this.labelFriends.AutoSize = true;
            this.labelFriends.ForeColor = System.Drawing.SystemColors.ButtonHighlight;
            this.labelFriends.Location = new System.Drawing.Point(171, 15);
            this.labelFriends.Name = "labelFriends";
            this.labelFriends.Size = new System.Drawing.Size(63, 13);
            this.labelFriends.TabIndex = 0;
            this.labelFriends.Text = "labelFriends";
            // 
            // FriendsWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = true;
            this.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.ClientSize = new System.Drawing.Size(264, 314);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.listBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FriendsWindow";
            this.Text = "Zero-K - Friends";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.FriendsWindow_FormClosed);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ListBox listBox1;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button buttonAddFriend;
        private System.Windows.Forms.Label labelFriends;
    }
}