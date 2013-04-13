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
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panel1.AutoScroll = true;
            this.panel1.BackColor = System.Drawing.SystemColors.Window;
            this.panel1.Location = new System.Drawing.Point(0, 24);
            this.panel1.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(664, 620);
            this.panel1.TabIndex = 0;
            this.panel1.TabStop = true;
            // 
            // applyButton
            // 
            this.applyButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.applyButton.Location = new System.Drawing.Point(0, 647);
            this.applyButton.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.applyButton.MaximumSize = new System.Drawing.Size(77, 50);
            this.applyButton.MinimumSize = new System.Drawing.Size(77, 50);
            this.applyButton.Name = "applyButton";
            this.applyButton.Size = new System.Drawing.Size(77, 50);
            this.applyButton.TabIndex = 1;
            this.applyButton.Text = "Save";
            this.applyButton.UseVisualStyleBackColor = true;
            this.applyButton.Click += new System.EventHandler(this.applyButton_Click);
            // 
            // cancelButton
            // 
            this.cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.cancelButton.Location = new System.Drawing.Point(77, 647);
            this.cancelButton.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.cancelButton.MaximumSize = new System.Drawing.Size(77, 50);
            this.cancelButton.MinimumSize = new System.Drawing.Size(77, 50);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(77, 50);
            this.cancelButton.TabIndex = 2;
            this.cancelButton.Text = "Exit";
            this.cancelButton.UseVisualStyleBackColor = true;
            this.cancelButton.Click += new System.EventHandler(this.cancelButton_Click);
            // 
            // engineDefaultButton
            // 
            this.engineDefaultButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.engineDefaultButton.Location = new System.Drawing.Point(156, 647);
            this.engineDefaultButton.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.engineDefaultButton.MaximumSize = new System.Drawing.Size(75, 50);
            this.engineDefaultButton.MinimumSize = new System.Drawing.Size(77, 50);
            this.engineDefaultButton.Name = "engineDefaultButton";
            this.engineDefaultButton.Size = new System.Drawing.Size(77, 50);
            this.engineDefaultButton.TabIndex = 3;
            this.engineDefaultButton.Text = "Load Default";
            this.engineDefaultButton.UseVisualStyleBackColor = true;
            this.engineDefaultButton.Click += new System.EventHandler(this.engineDefaultButton_Click);
            // 
            // doneLabel
            // 
            this.doneLabel.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.doneLabel.AutoSize = true;
            this.doneLabel.Location = new System.Drawing.Point(441, 647);
            this.doneLabel.Name = "doneLabel";
            this.doneLabel.Padding = new System.Windows.Forms.Padding(0, 15, 11, 0);
            this.doneLabel.Size = new System.Drawing.Size(114, 32);
            this.doneLabel.TabIndex = 4;
            this.doneLabel.Text = "Setting Applied";
            this.doneLabel.Visible = false;
            // 
            // loadDefaultDone
            // 
            this.loadDefaultDone.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.loadDefaultDone.AutoSize = true;
            this.loadDefaultDone.Location = new System.Drawing.Point(399, 647);
            this.loadDefaultDone.Name = "loadDefaultDone";
            this.loadDefaultDone.Padding = new System.Windows.Forms.Padding(0, 15, 11, 0);
            this.loadDefaultDone.Size = new System.Drawing.Size(156, 32);
            this.loadDefaultDone.TabIndex = 5;
            this.loadDefaultDone.Text = "Loaded Default Value";
            this.loadDefaultDone.Visible = false;
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(171, -1);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(188, 22);
            this.textBox1.TabIndex = 0;
            this.textBox1.TextChanged += new System.EventHandler(this.textBox1_TextChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(153, 17);
            this.label1.TabIndex = 6;
            this.label1.Text = "Highlight key with term:";
            // 
            // SpringsettingForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = true;
            this.ClientSize = new System.Drawing.Size(664, 698);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.loadDefaultDone);
            this.Controls.Add(this.doneLabel);
            this.Controls.Add(this.engineDefaultButton);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.applyButton);
            this.Controls.Add(this.panel1);
            this.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
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
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Label label1;


    }
}
