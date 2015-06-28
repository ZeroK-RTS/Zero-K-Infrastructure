namespace ZeroKLobby
{
    partial class SelectWritableFolder
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
            this.tbFolder = new System.Windows.Forms.TextBox();
            this.lbDescription = new System.Windows.Forms.Label();
            this.btnOk = new ZeroKLobby.BitmapButton();
            this.btnCancel = new ZeroKLobby.BitmapButton();
            this.browseFolderbutton = new ZeroKLobby.BitmapButton();
            this.SuspendLayout();
            // 
            // tbFolder
            // 
            this.tbFolder.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbFolder.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.tbFolder.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.FileSystemDirectories;
            this.tbFolder.Location = new System.Drawing.Point(12, 36);
            this.tbFolder.Name = "tbFolder";
            this.tbFolder.Size = new System.Drawing.Size(431, 20);
            this.tbFolder.TabIndex = 0;
            // 
            // lbDescription
            // 
            this.lbDescription.AutoSize = true;
            this.lbDescription.Location = new System.Drawing.Point(12, 13);
            this.lbDescription.Name = "lbDescription";
            this.lbDescription.Size = new System.Drawing.Size(194, 13);
            this.lbDescription.TabIndex = 1;
            this.lbDescription.Text = "Please select a folder for game content:";
            // 
            // btnOk
            // 
            this.btnOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOk.BackColor = System.Drawing.Color.Transparent;
            this.btnOk.BackgroundImage = global::ZeroKLobby.Buttons.panel;
            this.btnOk.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btnOk.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnOk.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnOk.ForeColor = System.Drawing.Color.White;
            this.btnOk.Location = new System.Drawing.Point(287, 74);
            this.btnOk.Margin = new System.Windows.Forms.Padding(0);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(75, 23);
            this.btnOk.TabIndex = 3;
            this.btnOk.Text = "OK";
            this.btnOk.UseVisualStyleBackColor = true;
            this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.BackColor = System.Drawing.Color.Transparent;
            this.btnCancel.BackgroundImage = global::ZeroKLobby.Buttons.panel;
            this.btnCancel.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btnCancel.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCancel.ForeColor = System.Drawing.Color.White;
            this.btnCancel.Location = new System.Drawing.Point(368, 74);
            this.btnCancel.Margin = new System.Windows.Forms.Padding(0);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 4;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // browseFolderbutton
            // 
            this.browseFolderbutton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.browseFolderbutton.BackColor = System.Drawing.Color.Transparent;
            this.browseFolderbutton.BackgroundImage = global::ZeroKLobby.Buttons.panel;
            this.browseFolderbutton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.browseFolderbutton.Cursor = System.Windows.Forms.Cursors.Hand;
            this.browseFolderbutton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.browseFolderbutton.ForeColor = System.Drawing.Color.White;
            this.browseFolderbutton.Location = new System.Drawing.Point(15, 74);
            this.browseFolderbutton.Margin = new System.Windows.Forms.Padding(0);
            this.browseFolderbutton.Name = "browseFolderbutton";
            this.browseFolderbutton.Size = new System.Drawing.Size(86, 23);
            this.browseFolderbutton.TabIndex = 5;
            this.browseFolderbutton.Text = "Browse Folder";
            this.browseFolderbutton.UseVisualStyleBackColor = true;
            this.browseFolderbutton.Click += new System.EventHandler(this.browseFolderbutton_Click);
            // 
            // SelectWritableFolder
            // 
            this.AcceptButton = this.btnOk;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.DimGray;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(455, 105);
            this.Controls.Add(this.browseFolderbutton);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.lbDescription);
            this.Controls.Add(this.tbFolder);
            this.ForeColor = System.Drawing.Color.White;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SelectWritableFolder";
            this.Text = "Please select a writable folder";
            this.Load += new System.EventHandler(this.SelectWritableFolder_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox tbFolder;
        private System.Windows.Forms.Label lbDescription;
        private BitmapButton btnOk;
        private BitmapButton btnCancel;
        private BitmapButton browseFolderbutton;
    }
}