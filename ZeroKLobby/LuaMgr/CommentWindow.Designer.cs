namespace ZeroKLobby.LuaMgr
{
    partial class CommentWindow
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
            this.components = new System.ComponentModel.Container();
            this.textBoxComments = new System.Windows.Forms.TextBox();
            this.textBoxAddComment = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.buttonCommentSend = new System.Windows.Forms.Button();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.SuspendLayout();
            // 
            // textBoxComments
            // 
            this.textBoxComments.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxComments.Location = new System.Drawing.Point(0, 0);
            this.textBoxComments.Multiline = true;
            this.textBoxComments.Name = "textBoxComments";
            this.textBoxComments.ReadOnly = true;
            this.textBoxComments.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textBoxComments.Size = new System.Drawing.Size(545, 170);
            this.textBoxComments.TabIndex = 0;
            // 
            // textBoxAddComment
            // 
            this.textBoxAddComment.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxAddComment.Location = new System.Drawing.Point(0, 199);
            this.textBoxAddComment.Multiline = true;
            this.textBoxAddComment.Name = "textBoxAddComment";
            this.textBoxAddComment.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textBoxAddComment.Size = new System.Drawing.Size(452, 63);
            this.textBoxAddComment.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(3, 181);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(98, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Add your comment:";
            // 
            // buttonCommentSend
            // 
            this.buttonCommentSend.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonCommentSend.Location = new System.Drawing.Point(460, 219);
            this.buttonCommentSend.Name = "buttonCommentSend";
            this.buttonCommentSend.Size = new System.Drawing.Size(75, 23);
            this.buttonCommentSend.TabIndex = 3;
            this.buttonCommentSend.Text = "Send";
            this.buttonCommentSend.UseVisualStyleBackColor = true;
            this.buttonCommentSend.Click += new System.EventHandler(this.buttonCommentSend_Click);
            // 
            // CommentWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(545, 261);
            this.Controls.Add(this.buttonCommentSend);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.textBoxAddComment);
            this.Controls.Add(this.textBoxComments);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.Name = "CommentWindow";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Comments";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox textBoxComments;
        private System.Windows.Forms.TextBox textBoxAddComment;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button buttonCommentSend;
        private System.Windows.Forms.ToolTip toolTip1;
    }
}