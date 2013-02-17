namespace ZeroKLobby.MicroLobby
{
    partial class SpringsettingForm
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
            this.panel1 = new System.Windows.Forms.Panel();
            this.applyButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.engineDefaultButton = new System.Windows.Forms.Button();
            this.doneLabel = new System.Windows.Forms.Label();
            this.loadDefaultDone = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.panel1.AutoScroll = true;
            this.panel1.BackColor = System.Drawing.SystemColors.Window;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Margin = new System.Windows.Forms.Padding(2);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(498, 523);
            this.panel1.TabIndex = 0;
            this.panel1.TabStop = true;
            // 
            // applyButton
            // 
            this.applyButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.applyButton.Location = new System.Drawing.Point(0, 526);
            this.applyButton.Margin = new System.Windows.Forms.Padding(2);
            this.applyButton.MaximumSize = new System.Drawing.Size(58, 41);
            this.applyButton.MinimumSize = new System.Drawing.Size(58, 41);
            this.applyButton.Name = "applyButton";
            this.applyButton.Size = new System.Drawing.Size(58, 41);
            this.applyButton.TabIndex = 1;
            this.applyButton.Text = "Save";
            this.applyButton.UseVisualStyleBackColor = true;
            this.applyButton.Click += new System.EventHandler(this.applyButton_Click);
            // 
            // cancelButton
            // 
            this.cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.cancelButton.Location = new System.Drawing.Point(58, 526);
            this.cancelButton.Margin = new System.Windows.Forms.Padding(2);
            this.cancelButton.MaximumSize = new System.Drawing.Size(58, 41);
            this.cancelButton.MinimumSize = new System.Drawing.Size(58, 41);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(58, 41);
            this.cancelButton.TabIndex = 2;
            this.cancelButton.Text = "Exit";
            this.cancelButton.UseVisualStyleBackColor = true;
            this.cancelButton.Click += new System.EventHandler(this.cancelButton_Click);
            // 
            // engineDefaultButton
            // 
            this.engineDefaultButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.engineDefaultButton.Location = new System.Drawing.Point(117, 526);
            this.engineDefaultButton.Margin = new System.Windows.Forms.Padding(2);
            this.engineDefaultButton.MaximumSize = new System.Drawing.Size(56, 41);
            this.engineDefaultButton.MinimumSize = new System.Drawing.Size(58, 41);
            this.engineDefaultButton.Name = "engineDefaultButton";
            this.engineDefaultButton.Size = new System.Drawing.Size(58, 41);
            this.engineDefaultButton.TabIndex = 3;
            this.engineDefaultButton.Text = "Load Default";
            this.engineDefaultButton.UseVisualStyleBackColor = true;
            this.engineDefaultButton.Click += new System.EventHandler(this.engineDefaultButton_Click);
            // 
            // doneLabel
            // 
            this.doneLabel.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.doneLabel.AutoSize = true;
            this.doneLabel.Location = new System.Drawing.Point(331, 526);
            this.doneLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.doneLabel.Name = "doneLabel";
            this.doneLabel.Padding = new System.Windows.Forms.Padding(0, 12, 8, 0);
            this.doneLabel.Size = new System.Drawing.Size(86, 25);
            this.doneLabel.TabIndex = 4;
            this.doneLabel.Text = "Setting Applied";
            this.doneLabel.Visible = false;
            // 
            // loadDefaultDone
            // 
            this.loadDefaultDone.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.loadDefaultDone.AutoSize = true;
            this.loadDefaultDone.Location = new System.Drawing.Point(299, 526);
            this.loadDefaultDone.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.loadDefaultDone.Name = "loadDefaultDone";
            this.loadDefaultDone.Padding = new System.Windows.Forms.Padding(0, 12, 8, 0);
            this.loadDefaultDone.Size = new System.Drawing.Size(118, 25);
            this.loadDefaultDone.TabIndex = 5;
            this.loadDefaultDone.Text = "Loaded Default Value";
            this.loadDefaultDone.Visible = false;
            // 
            // SpringsettingForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = true;
            this.ClientSize = new System.Drawing.Size(498, 567);
            this.Controls.Add(this.loadDefaultDone);
            this.Controls.Add(this.doneLabel);
            this.Controls.Add(this.engineDefaultButton);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.applyButton);
            this.Controls.Add(this.panel1);
            this.Margin = new System.Windows.Forms.Padding(2);
            this.Name = "SpringsettingForm";
            this.Text = "Springsetting.cfg";
            this.Load += new System.EventHandler(this.SpringsettingForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button applyButton;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.Button engineDefaultButton;
        private System.Windows.Forms.Label doneLabel;
        private System.Windows.Forms.Label loadDefaultDone;


    }
}
