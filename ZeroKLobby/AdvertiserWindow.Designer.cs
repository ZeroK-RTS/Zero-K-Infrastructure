namespace ZeroKLobby
{
    partial class AdvertiserWindow
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
            this.cbActive = new System.Windows.Forms.CheckBox();
            this.tbDelay = new System.Windows.Forms.TextBox();
            this.tbAdlines = new System.Windows.Forms.TextBox();
            this.btnNow = new ZeroKLobby.BitmapButton();
            this.button2 = new ZeroKLobby.BitmapButton();
            this.tbSuffix = new System.Windows.Forms.TextBox();
            this.tbChannels = new System.Windows.Forms.TextBox();
            this.tbPrefix = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // cbActive
            // 
            this.cbActive.AutoSize = true;
            this.cbActive.Location = new System.Drawing.Point(28, 26);
            this.cbActive.Name = "cbActive";
            this.cbActive.Size = new System.Drawing.Size(56, 17);
            this.cbActive.TabIndex = 0;
            this.cbActive.Text = "Active";
            this.cbActive.UseVisualStyleBackColor = true;
            // 
            // tbDelay
            // 
            this.tbDelay.Location = new System.Drawing.Point(114, 23);
            this.tbDelay.Name = "tbDelay";
            this.tbDelay.Size = new System.Drawing.Size(100, 20);
            this.tbDelay.TabIndex = 1;
            this.tbDelay.TextChanged += new System.EventHandler(this.tbDelay_TextChanged);
            // 
            // tbAdlines
            // 
            this.tbAdlines.Location = new System.Drawing.Point(28, 107);
            this.tbAdlines.Multiline = true;
            this.tbAdlines.Name = "tbAdlines";
            this.tbAdlines.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.tbAdlines.Size = new System.Drawing.Size(264, 311);
            this.tbAdlines.TabIndex = 2;
            this.tbAdlines.TextChanged += new System.EventHandler(this.tbAdlines_TextChanged);
            // 
            // btnNow
            // 
            this.btnNow.Location = new System.Drawing.Point(236, 21);
            this.btnNow.Name = "btnNow";
            this.btnNow.Size = new System.Drawing.Size(75, 23);
            this.btnNow.TabIndex = 3;
            this.btnNow.Text = "Say now";
            this.btnNow.UseVisualStyleBackColor = true;
            this.btnNow.Click += new System.EventHandler(this.btnNow_Click);
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(28, 78);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(75, 23);
            this.button2.TabIndex = 4;
            this.button2.Text = "Load lines";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // tbSuffix
            // 
            this.tbSuffix.Location = new System.Drawing.Point(326, 300);
            this.tbSuffix.Multiline = true;
            this.tbSuffix.Name = "tbSuffix";
            this.tbSuffix.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.tbSuffix.Size = new System.Drawing.Size(202, 118);
            this.tbSuffix.TabIndex = 6;
            this.tbSuffix.TextChanged += new System.EventHandler(this.tbSuffix_TextChanged);
            // 
            // tbChannels
            // 
            this.tbChannels.Location = new System.Drawing.Point(326, 107);
            this.tbChannels.Name = "tbChannels";
            this.tbChannels.Size = new System.Drawing.Size(188, 20);
            this.tbChannels.TabIndex = 7;
            this.tbChannels.TextChanged += new System.EventHandler(this.tbChannels_TextChanged);
            // 
            // tbPrefix
            // 
            this.tbPrefix.Location = new System.Drawing.Point(326, 167);
            this.tbPrefix.Multiline = true;
            this.tbPrefix.Name = "tbPrefix";
            this.tbPrefix.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.tbPrefix.Size = new System.Drawing.Size(202, 109);
            this.tbPrefix.TabIndex = 8;
            this.tbPrefix.TextChanged += new System.EventHandler(this.tbPrefix_TextChanged);
            // 
            // AdvertiserWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tbPrefix);
            this.Controls.Add(this.tbChannels);
            this.Controls.Add(this.tbSuffix);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.btnNow);
            this.Controls.Add(this.tbAdlines);
            this.Controls.Add(this.tbDelay);
            this.Controls.Add(this.cbActive);
            this.Name = "AdvertiserWindow";
            this.Size = new System.Drawing.Size(644, 443);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckBox cbActive;
        private System.Windows.Forms.TextBox tbDelay;
        private System.Windows.Forms.TextBox tbAdlines;
        private System.Windows.Forms.Button btnNow;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.TextBox tbSuffix;
        private System.Windows.Forms.TextBox tbChannels;
        private System.Windows.Forms.TextBox tbPrefix;
    }
}