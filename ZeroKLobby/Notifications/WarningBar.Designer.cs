namespace ZeroKLobby.Notifications
{
    partial class WarningBar
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
            this.lbText = new System.Windows.Forms.Label();
            this.bitmapButton1 = new ZeroKLobby.BitmapButton();
            this.SuspendLayout();
            // 
            // lbText
            // 
            this.lbText.AutoSize = true;
            this.lbText.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.lbText.Location = new System.Drawing.Point(21, 20);
            this.lbText.Name = "lbText";
            this.lbText.Size = new System.Drawing.Size(46, 15);
            this.lbText.TabIndex = 0;
            this.lbText.Text = "lbText";
            // 
            // bitmapButton1
            // 
            this.bitmapButton1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.bitmapButton1.BackColor = System.Drawing.Color.Transparent;
            this.bitmapButton1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.bitmapButton1.ButtonStyle = ZeroKLobby.FrameBorderRenderer.StyleType.DarkHive;
            this.bitmapButton1.Cursor = System.Windows.Forms.Cursors.Hand;
            this.bitmapButton1.FlatAppearance.BorderSize = 0;
            this.bitmapButton1.FlatAppearance.CheckedBackColor = System.Drawing.Color.Transparent;
            this.bitmapButton1.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Transparent;
            this.bitmapButton1.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Transparent;
            this.bitmapButton1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.bitmapButton1.ForeColor = System.Drawing.Color.White;
            this.bitmapButton1.Location = new System.Drawing.Point(917, 12);
            this.bitmapButton1.Name = "bitmapButton1";
            this.bitmapButton1.Size = new System.Drawing.Size(82, 34);
            this.bitmapButton1.SoundType = ZeroKLobby.Controls.SoundPalette.SoundType.Click;
            this.bitmapButton1.TabIndex = 1;
            this.bitmapButton1.Text = "OK";
            this.bitmapButton1.UseVisualStyleBackColor = false;
            this.bitmapButton1.Click += new System.EventHandler(this.bitmapButton1_Click);
            // 
            // WarningBar
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 18F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Transparent;
            this.Controls.Add(this.bitmapButton1);
            this.Controls.Add(this.lbText);
            this.Name = "WarningBar";
            this.Size = new System.Drawing.Size(1017, 59);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lbText;
        private BitmapButton bitmapButton1;
    }
}
