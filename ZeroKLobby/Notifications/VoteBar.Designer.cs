namespace ZeroKLobby.Notifications
{
    partial class VoteBar
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
            this.pbYes = new System.Windows.Forms.ProgressBar();
            this.pbNo = new System.Windows.Forms.ProgressBar();
            this.lbYes = new System.Windows.Forms.Label();
            this.lbNo = new System.Windows.Forms.Label();
            this.btnYes = new System.Windows.Forms.Button();
            this.btnNo = new System.Windows.Forms.Button();
            this.lbQuestion = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // pbYes
            // 
            this.pbYes.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pbYes.ForeColor = System.Drawing.Color.LimeGreen;
            this.pbYes.Location = new System.Drawing.Point(86, 25);
            this.pbYes.Name = "pbYes";
            this.pbYes.Size = new System.Drawing.Size(310, 15);
            this.pbYes.TabIndex = 0;
            // 
            // pbNo
            // 
            this.pbNo.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pbNo.ForeColor = System.Drawing.Color.Red;
            this.pbNo.Location = new System.Drawing.Point(86, 46);
            this.pbNo.Name = "pbNo";
            this.pbNo.Size = new System.Drawing.Size(310, 15);
            this.pbNo.TabIndex = 1;
            // 
            // lbYes
            // 
            this.lbYes.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lbYes.AutoSize = true;
            this.lbYes.Location = new System.Drawing.Point(402, 25);
            this.lbYes.Name = "lbYes";
            this.lbYes.Size = new System.Drawing.Size(35, 13);
            this.lbYes.TabIndex = 3;
            this.lbYes.Text = "label1";
            // 
            // lbNo
            // 
            this.lbNo.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lbNo.AutoSize = true;
            this.lbNo.Location = new System.Drawing.Point(402, 48);
            this.lbNo.Name = "lbNo";
            this.lbNo.Size = new System.Drawing.Size(35, 13);
            this.lbNo.TabIndex = 4;
            this.lbNo.Text = "label2";
            // 
            // btnYes
            // 
            this.btnYes.Location = new System.Drawing.Point(17, 25);
            this.btnYes.Name = "btnYes";
            this.btnYes.Size = new System.Drawing.Size(41, 20);
            this.btnYes.TabIndex = 5;
            this.btnYes.Text = "Yes";
            this.btnYes.UseVisualStyleBackColor = true;
            this.btnYes.Click += new System.EventHandler(this.btnYes_Click);
            // 
            // btnNo
            // 
            this.btnNo.Location = new System.Drawing.Point(17, 46);
            this.btnNo.Name = "btnNo";
            this.btnNo.Size = new System.Drawing.Size(41, 20);
            this.btnNo.TabIndex = 6;
            this.btnNo.Text = "No";
            this.btnNo.UseVisualStyleBackColor = true;
            this.btnNo.Click += new System.EventHandler(this.btnNo_Click);
            // 
            // lbQuestion
            // 
            this.lbQuestion.AutoSize = true;
            this.lbQuestion.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.lbQuestion.Location = new System.Drawing.Point(83, 9);
            this.lbQuestion.Name = "lbQuestion";
            this.lbQuestion.Size = new System.Drawing.Size(41, 13);
            this.lbQuestion.TabIndex = 2;
            this.lbQuestion.Text = "label1";
            // 
            // VoteBar
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.btnNo);
            this.Controls.Add(this.btnYes);
            this.Controls.Add(this.lbNo);
            this.Controls.Add(this.lbYes);
            this.Controls.Add(this.lbQuestion);
            this.Controls.Add(this.pbNo);
            this.Controls.Add(this.pbYes);
            this.Name = "VoteBar";
            this.Size = new System.Drawing.Size(480, 80);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ProgressBar pbYes;
        private System.Windows.Forms.ProgressBar pbNo;
        private System.Windows.Forms.Label lbYes;
        private System.Windows.Forms.Label lbNo;
        private System.Windows.Forms.Button btnYes;
        private System.Windows.Forms.Button btnNo;
        private System.Windows.Forms.Label lbQuestion;
    }
}
