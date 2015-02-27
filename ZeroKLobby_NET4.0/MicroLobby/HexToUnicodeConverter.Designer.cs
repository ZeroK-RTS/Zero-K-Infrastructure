namespace ZeroKLobby.MicroLobby
{
    partial class HexToUnicodeConverter
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(HexToUnicodeConverter));
            this.label1 = new System.Windows.Forms.Label();
            this.linkLabel1 = new System.Windows.Forms.LinkLabel();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.sendBox2 = new ZeroKLobby.MicroLobby.SendBox();
            this.sendBox1 = new ZeroKLobby.MicroLobby.SendBox();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(2, 5);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(24, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = " U+";
            // 
            // linkLabel1
            // 
            this.linkLabel1.AutoSize = true;
            this.linkLabel1.Location = new System.Drawing.Point(2, 62);
            this.linkLabel1.Name = "linkLabel1";
            this.linkLabel1.Size = new System.Drawing.Size(171, 13);
            this.linkLabel1.TabIndex = 3;
            this.linkLabel1.TabStop = true;
            this.linkLabel1.Text = "http://unicode-table.com/en/sets/";
            this.linkLabel1.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabel1_LinkClicked);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(2, 49);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(149, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "Visit link for list of symbols:";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(2, 31);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(42, 13);
            this.label3.TabIndex = 5;
            this.label3.Text = "Output:";
            // 
            // sendBox2
            // 
            this.sendBox2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
            this.sendBox2.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
            this.sendBox2.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.sendBox2.Location = new System.Drawing.Point(32, 0);
            this.sendBox2.Multiline = true;
            this.sendBox2.Name = "sendBox2";
            this.sendBox2.Size = new System.Drawing.Size(128, 20);
            this.sendBox2.TabIndex = 0;
            this.sendBox2.WordWrap = false;
            this.sendBox2.TextChanged += new System.EventHandler(this.sendBox2_TextChanged);
            this.sendBox2.Enter += new System.EventHandler(this.sendBox2_Enter);
            // 
            // sendBox1
            // 
            this.sendBox1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
            this.sendBox1.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
            this.sendBox1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.sendBox1.Location = new System.Drawing.Point(45, 26);
            this.sendBox1.Multiline = true;
            this.sendBox1.Name = "sendBox1";
            this.sendBox1.Size = new System.Drawing.Size(128, 20);
            this.sendBox1.TabIndex = 1;
            this.sendBox1.WordWrap = false;
            this.sendBox1.TextChanged += new System.EventHandler(this.sendBox1_TextChanged);
            this.sendBox1.Enter += new System.EventHandler(this.sendBox1_Enter);
            // 
            // HexToUnicodeConverter
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(173, 76);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.linkLabel1);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.sendBox2);
            this.Controls.Add(this.sendBox1);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "HexToUnicodeConverter";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Hex-To-Unicode";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private SendBox sendBox1;
        private SendBox sendBox2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.LinkLabel linkLabel1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
    }
}