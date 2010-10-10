namespace SpringDownloader.Notifications
{
    partial class NewVersionBar
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
					this.progressBar1 = new System.Windows.Forms.ProgressBar();
					this.SuspendLayout();
					// 
					// lbText
					// 
					this.lbText.AutoSize = true;
					this.lbText.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
					this.lbText.Location = new System.Drawing.Point(13, 15);
					this.lbText.Name = "lbText";
					this.lbText.Size = new System.Drawing.Size(35, 13);
					this.lbText.TabIndex = 0;
					this.lbText.Text = "label1";
					// 
					// progressBar1
					// 
					this.progressBar1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
											| System.Windows.Forms.AnchorStyles.Right)));
					this.progressBar1.Location = new System.Drawing.Point(16, 31);
					this.progressBar1.Name = "progressBar1";
					this.progressBar1.Size = new System.Drawing.Size(232, 15);
					this.progressBar1.TabIndex = 1;
					// 
					// NewVersionBar
					// 
					this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
					this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
					this.Controls.Add(this.progressBar1);
					this.Controls.Add(this.lbText);
					this.Name = "NewVersionBar";
					this.Size = new System.Drawing.Size(264, 53);
					this.ResumeLayout(false);
					this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lbText;
				private System.Windows.Forms.ProgressBar progressBar1;
    }
}
