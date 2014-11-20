namespace ZeroKLobby.MicroForms
{
    partial class PromptForm
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
            this.rememberChoiceCheckbox = new System.Windows.Forms.CheckBox();
            this.okButton = new ZeroKLobby.BitmapButton();
            this.noButton = new ZeroKLobby.BitmapButton();
            this.questionText = new System.Windows.Forms.TextBox();
            this.detailBox = new System.Windows.Forms.GroupBox();
            this.detailText = new System.Windows.Forms.TextBox();
            this.detailBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // rememberChoiceCheckbox
            // 
            this.rememberChoiceCheckbox.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.rememberChoiceCheckbox.AutoSize = true;
            this.rememberChoiceCheckbox.Location = new System.Drawing.Point(53, 87);
            this.rememberChoiceCheckbox.Name = "rememberChoiceCheckbox";
            this.rememberChoiceCheckbox.Size = new System.Drawing.Size(100, 17);
            this.rememberChoiceCheckbox.TabIndex = 0;
            this.rememberChoiceCheckbox.Text = "Don\'t ask again";
            this.rememberChoiceCheckbox.UseVisualStyleBackColor = true;
            // 
            // okButton
            // 
            this.okButton.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.okButton.BackColor = System.Drawing.Color.Transparent;
            this.okButton.BackgroundImage = global::ZeroKLobby.Buttons.panel;
            this.okButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.okButton.Cursor = System.Windows.Forms.Cursors.Hand;
            this.okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.okButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.okButton.ForeColor = System.Drawing.Color.White;
            this.okButton.Location = new System.Drawing.Point(39, 58);
            this.okButton.Margin = new System.Windows.Forms.Padding(0);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(62, 23);
            this.okButton.TabIndex = 2;
            this.okButton.Text = "Yes";
            this.okButton.UseVisualStyleBackColor = true;
            // 
            // noButton
            // 
            this.noButton.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.noButton.BackColor = System.Drawing.Color.Transparent;
            this.noButton.BackgroundImage = global::ZeroKLobby.Buttons.panel;
            this.noButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.noButton.Cursor = System.Windows.Forms.Cursors.Hand;
            this.noButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.noButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.noButton.ForeColor = System.Drawing.Color.White;
            this.noButton.Location = new System.Drawing.Point(104, 58);
            this.noButton.Margin = new System.Windows.Forms.Padding(0);
            this.noButton.Name = "noButton";
            this.noButton.Size = new System.Drawing.Size(62, 23);
            this.noButton.TabIndex = 1;
            this.noButton.Text = "No";
            this.noButton.UseVisualStyleBackColor = true;
            // 
            // questionText
            // 
            this.questionText.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.questionText.BackColor = System.Drawing.Color.DimGray;
            this.questionText.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.questionText.ForeColor = System.Drawing.Color.White;
            this.questionText.Location = new System.Drawing.Point(0, 2);
            this.questionText.Multiline = true;
            this.questionText.Name = "questionText";
            this.questionText.ReadOnly = true;
            this.questionText.Size = new System.Drawing.Size(206, 53);
            this.questionText.TabIndex = 8;
            this.questionText.Text = "question?/decision?\r\n1\r\n2\r\n3";
            this.questionText.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // detailBox
            // 
            this.detailBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.detailBox.Controls.Add(this.detailText);
            this.detailBox.Location = new System.Drawing.Point(3, 111);
            this.detailBox.Name = "detailBox";
            this.detailBox.Size = new System.Drawing.Size(203, 101);
            this.detailBox.TabIndex = 9;
            this.detailBox.TabStop = false;
            this.detailBox.Text = "Detail:";
            this.detailBox.VisibleChanged += new System.EventHandler(this.detailBox_VisibleChanged);
            // 
            // detailText
            // 
            this.detailText.BackColor = System.Drawing.Color.DimGray;
            this.detailText.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.detailText.Dock = System.Windows.Forms.DockStyle.Fill;
            this.detailText.ForeColor = System.Drawing.Color.White;
            this.detailText.Location = new System.Drawing.Point(3, 16);
            this.detailText.Multiline = true;
            this.detailText.Name = "detailText";
            this.detailText.ReadOnly = true;
            this.detailText.Size = new System.Drawing.Size(197, 82);
            this.detailText.TabIndex = 0;
            this.detailText.Text = "FileName:xxx\r\nMD5:yyy\r\nInternalName:zzz";
            // 
            // PromptForm
            // 
            this.AcceptButton = this.okButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.DimGray;
            this.CancelButton = this.noButton;
            this.ClientSize = new System.Drawing.Size(208, 213);
            this.ControlBox = false;
            this.Controls.Add(this.detailBox);
            this.Controls.Add(this.questionText);
            this.Controls.Add(this.noButton);
            this.Controls.Add(this.okButton);
            this.Controls.Add(this.rememberChoiceCheckbox);
            this.ForeColor = System.Drawing.Color.White;
            this.MaximizeBox = false;
            this.MaximumSize = new System.Drawing.Size(400, 248);
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(216, 147);
            this.Name = "PromptForm";
            this.ShowIcon = false;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "title/event!";
            this.TopMost = true;
            this.detailBox.ResumeLayout(false);
            this.detailBox.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        public System.Windows.Forms.CheckBox rememberChoiceCheckbox;
        public System.Windows.Forms.TextBox questionText;
        public System.Windows.Forms.GroupBox detailBox;
        public System.Windows.Forms.TextBox detailText;
        public BitmapButton okButton;
        public BitmapButton noButton;

    }
}