namespace ZeroKLobby.MicroLobby
{
    partial class PrivateMessageControl
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
            this.sendBox = new SendBox();
            this.ChatBox = new ChatBox();
            this.SuspendLayout();
            // 
            // sendBox
            // 
            this.sendBox.AcceptsTab = true;
            this.sendBox.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.sendBox.Location = new System.Drawing.Point(0, 389);
            this.sendBox.Multiline = true;
            this.sendBox.Name = "sendBox";
            this.sendBox.Size = new System.Drawing.Size(455, 20);
            this.sendBox.TabIndex = 0;
            // 
            // chatBox
            // 
            this.ChatBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ChatBox.Location = new System.Drawing.Point(0, 0);
            this.ChatBox.Name = "chatBox";
            this.ChatBox.Size = new System.Drawing.Size(455, 389);
            this.ChatBox.TabIndex = 1;
            this.ChatBox.Text = "";
            // 
            // PrivateMessageControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.ChatBox);
            this.Controls.Add(this.sendBox);
            this.Name = "PrivateMessageControl";
            this.Size = new System.Drawing.Size(455, 409);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        public SendBox sendBox;
        public ChatBox ChatBox { get; set; }
    }
}
