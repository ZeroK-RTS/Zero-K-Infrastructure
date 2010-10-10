namespace SpringDownloader.Notifications
{
    partial class DownloadBar
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
            this.components = new System.ComponentModel.Container();
            this.label = new System.Windows.Forms.Label();
            this.progress = new System.Windows.Forms.ProgressBar();
            this.minimapBox = new System.Windows.Forms.PictureBox();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.minimapBox)).BeginInit();
            this.SuspendLayout();
            // 
            // label
            // 
            this.label.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                                                                      | System.Windows.Forms.AnchorStyles.Right)));
            this.label.AutoEllipsis = true;
            this.label.BackColor = System.Drawing.Color.Transparent;
            this.label.Location = new System.Drawing.Point(6, 3);
            this.label.Margin = new System.Windows.Forms.Padding(0);
            this.label.Name = "label";
            this.label.Size = new System.Drawing.Size(294, 13);
            this.label.TabIndex = 0;
            this.label.Text = "Loading ...";
            // 
            // progress
            // 
            this.progress.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                                                                         | System.Windows.Forms.AnchorStyles.Right)));
            this.progress.Location = new System.Drawing.Point(3, 19);
            this.progress.MarqueeAnimationSpeed = 50;
            this.progress.Maximum = 10000;
            this.progress.Name = "progress";
            this.progress.Size = new System.Drawing.Size(294, 10);
            this.progress.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            this.progress.TabIndex = 1;
            // 
            // minimapBox
            // 
            this.minimapBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                                                                           | System.Windows.Forms.AnchorStyles.Right)));
            this.minimapBox.Cursor = System.Windows.Forms.Cursors.Hand;
            this.minimapBox.Location = new System.Drawing.Point(300, 0);
            this.minimapBox.Name = "minimapBox";
            this.minimapBox.Size = new System.Drawing.Size(0, 32);
            this.minimapBox.TabIndex = 4;
            this.minimapBox.TabStop = false;
            this.minimapBox.Click += new System.EventHandler(this.minimapBox_Click);
            // 
            // TransferStatus
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.minimapBox);
            this.Controls.Add(this.progress);
            this.Controls.Add(this.label);
            this.DoubleBuffered = true;
            this.Name = "TransferStatus";
            this.Size = new System.Drawing.Size(300, 32);
            ((System.ComponentModel.ISupportInitialize)(this.minimapBox)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label label;
        private System.Windows.Forms.ProgressBar progress;
        private System.Windows.Forms.PictureBox minimapBox;
        private System.Windows.Forms.ToolTip toolTip1;
    }
}