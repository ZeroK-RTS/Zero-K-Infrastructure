namespace ZeroKLobby.Controls.Campaign
{
    partial class JournalSubPanel
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
            this.journalImageBox = new System.Windows.Forms.PictureBox();
            this.journalTitleBox = new ZeroKLobby.Controls.TransparentTextBox();
            this.journalTextBox = new ZeroKLobby.Controls.TransparentTextBox();
            ((System.ComponentModel.ISupportInitialize)(this.journalImageBox)).BeginInit();
            this.SuspendLayout();
            // 
            // journalImageBox
            // 
            this.journalImageBox.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.journalImageBox.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.journalImageBox.Location = new System.Drawing.Point(0, 0);
            this.journalImageBox.Name = "journalImageBox";
            this.journalImageBox.Size = new System.Drawing.Size(480, 160);
            this.journalImageBox.TabIndex = 0;
            this.journalImageBox.TabStop = false;
            // 
            // journalTitleBox
            // 
            this.journalTitleBox.BackColor = System.Drawing.Color.DimGray;
            this.journalTitleBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.journalTitleBox.ForeColor = System.Drawing.Color.White;
            this.journalTitleBox.Location = new System.Drawing.Point(0, 166);
            this.journalTitleBox.Multiline = true;
            this.journalTitleBox.Name = "journalTitleBox";
            this.journalTitleBox.Size = new System.Drawing.Size(480, 35);
            this.journalTitleBox.TabIndex = 5;
            // 
            // journalTextBox
            // 
            this.journalTextBox.BackColor = System.Drawing.Color.DimGray;
            this.journalTextBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.journalTextBox.ForeColor = System.Drawing.Color.White;
            this.journalTextBox.Location = new System.Drawing.Point(0, 207);
            this.journalTextBox.Multiline = true;
            this.journalTextBox.Name = "journalTextBox";
            this.journalTextBox.Size = new System.Drawing.Size(480, 343);
            this.journalTextBox.TabIndex = 4;
            // 
            // JournalSubPanel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.BackColor = System.Drawing.Color.Transparent;
            this.Controls.Add(this.journalTitleBox);
            this.Controls.Add(this.journalTextBox);
            this.Controls.Add(this.journalImageBox);
            this.ForeColor = System.Drawing.Color.White;
            this.Name = "JournalSubPanel";
            this.Size = new System.Drawing.Size(483, 553);
            ((System.ComponentModel.ISupportInitialize)(this.journalImageBox)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox journalImageBox;
        private TransparentTextBox journalTextBox;
        private TransparentTextBox journalTitleBox;
    }
}
