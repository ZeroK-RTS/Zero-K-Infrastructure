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
            this.sendBox1 = new SendBox();
            this.ChatBox = new ChatBox();
            this.SuspendLayout();
            // 
            // sendBox1
            // 
            this.sendBox1.AcceptsTab = true;
            this.sendBox1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.sendBox1.Location = new System.Drawing.Point(0, 389);
            this.sendBox1.Multiline = true;
            this.sendBox1.Name = "sendBox1";
            this.sendBox1.Size = new System.Drawing.Size(455, 20);
            this.sendBox1.TabIndex = 0;
            this.sendBox1.PreviewKeyDown += new System.Windows.Forms.PreviewKeyDownEventHandler(this.sendBox1_PreviewKeyDown);
            this.sendBox1.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.sendBox1_KeyPress);
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
            this.Controls.Add(this.sendBox1);
            this.Name = "PrivateMessageControl";
            this.Size = new System.Drawing.Size(455, 409);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private SendBox sendBox1;
        public ChatBox ChatBox { get; set; }
    }
}
