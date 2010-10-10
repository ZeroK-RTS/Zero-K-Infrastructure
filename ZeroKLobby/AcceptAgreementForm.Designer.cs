namespace SpringDownloader
{
	partial class AcceptAgreementForm
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
			this.btnAgree = new System.Windows.Forms.Button();
			this.richTextBox1 = new System.Windows.Forms.RichTextBox();
			this.SuspendLayout();
			// 
			// btnAgree
			// 
			this.btnAgree.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.btnAgree.Location = new System.Drawing.Point(225, 457);
			this.btnAgree.Name = "btnAgree";
			this.btnAgree.Size = new System.Drawing.Size(75, 23);
			this.btnAgree.TabIndex = 1;
			this.btnAgree.Text = "Accept";
			this.btnAgree.UseVisualStyleBackColor = true;
			this.btnAgree.Click += new System.EventHandler(this.btnAgree_Click);
			// 
			// richTextBox1
			// 
			this.richTextBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
									| System.Windows.Forms.AnchorStyles.Left)
									| System.Windows.Forms.AnchorStyles.Right)));
			this.richTextBox1.Location = new System.Drawing.Point(0, 0);
			this.richTextBox1.Name = "richTextBox1";
			this.richTextBox1.ReadOnly = true;
			this.richTextBox1.Size = new System.Drawing.Size(534, 451);
			this.richTextBox1.TabIndex = 2;
			this.richTextBox1.Text = "";
			// 
			// AcceptAgreementForm
			// 
			this.AcceptButton = this.btnAgree;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(536, 486);
			this.Controls.Add(this.richTextBox1);
			this.Controls.Add(this.btnAgree);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "AcceptAgreementForm";
			this.Text = "Accept agreement";
			this.Load += new System.EventHandler(this.AcceptAgreementForm_Load);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Button btnAgree;
		private System.Windows.Forms.RichTextBox richTextBox1;
	}
}