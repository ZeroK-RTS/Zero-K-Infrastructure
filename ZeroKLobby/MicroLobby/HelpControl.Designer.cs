namespace SpringDownloader.MicroLobby
{
    partial class HelpControl
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
					this.webBrowser1 = new System.Windows.Forms.WebBrowser();
					this.helpButton = new System.Windows.Forms.Button();
					this.feedbackButton = new System.Windows.Forms.Button();
					this.problemButton = new System.Windows.Forms.Button();
					this.logButton = new System.Windows.Forms.Button();
					this.SuspendLayout();
					// 
					// webBrowser1
					// 
					this.webBrowser1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
											| System.Windows.Forms.AnchorStyles.Left)
											| System.Windows.Forms.AnchorStyles.Right)));
					this.webBrowser1.Location = new System.Drawing.Point(0, 32);
					this.webBrowser1.MinimumSize = new System.Drawing.Size(20, 20);
					this.webBrowser1.Name = "webBrowser1";
					this.webBrowser1.Size = new System.Drawing.Size(552, 393);
					this.webBrowser1.TabIndex = 0;
					this.webBrowser1.Url = new System.Uri("", System.UriKind.Relative);
					// 
					// helpButton
					// 
					this.helpButton.Location = new System.Drawing.Point(3, 3);
					this.helpButton.Name = "helpButton";
					this.helpButton.Size = new System.Drawing.Size(86, 23);
					this.helpButton.TabIndex = 1;
					this.helpButton.Text = "Ask for Help";
					this.helpButton.UseVisualStyleBackColor = true;
					// 
					// feedbackButton
					// 
					this.feedbackButton.Location = new System.Drawing.Point(334, 3);
					this.feedbackButton.Name = "feedbackButton";
					this.feedbackButton.Size = new System.Drawing.Size(187, 23);
					this.feedbackButton.TabIndex = 2;
					this.feedbackButton.Text = "Tell us what you\'d like to see in SD";
					this.feedbackButton.UseVisualStyleBackColor = true;
					// 
					// problemButton
					// 
					this.problemButton.Location = new System.Drawing.Point(95, 3);
					this.problemButton.Name = "problemButton";
					this.problemButton.Size = new System.Drawing.Size(103, 23);
					this.problemButton.TabIndex = 3;
					this.problemButton.Text = "Report a Problem";
					this.problemButton.UseVisualStyleBackColor = true;
					this.problemButton.Click += new System.EventHandler(this.problemButton_Click);
					// 
					// logButton
					// 
					this.logButton.Location = new System.Drawing.Point(204, 3);
					this.logButton.Name = "logButton";
					this.logButton.Size = new System.Drawing.Size(124, 23);
					this.logButton.TabIndex = 4;
					this.logButton.Text = "Show Diagnostic Log";
					this.logButton.UseVisualStyleBackColor = true;
					this.logButton.Click += new System.EventHandler(this.logButton_Click);
					// 
					// HelpControl
					// 
					this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
					this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
					this.Controls.Add(this.logButton);
					this.Controls.Add(this.problemButton);
					this.Controls.Add(this.feedbackButton);
					this.Controls.Add(this.helpButton);
					this.Controls.Add(this.webBrowser1);
					this.Name = "HelpControl";
					this.Size = new System.Drawing.Size(555, 425);
					this.Load += new System.EventHandler(this.HelpControl_Load);
					this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.WebBrowser webBrowser1;
        private System.Windows.Forms.Button helpButton;
        private System.Windows.Forms.Button feedbackButton;
        private System.Windows.Forms.Button problemButton;
        private System.Windows.Forms.Button logButton;
    }
}
