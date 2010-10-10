namespace ZeroKLobby.MicroLobby
{
    partial class SettingsTab
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
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.propertyGrid1 = new System.Windows.Forms.PropertyGrid();
            this.btnKeybindings = new System.Windows.Forms.Button();
            this.btnDisplay = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.propertyGrid1);
            this.groupBox1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.groupBox1.Location = new System.Drawing.Point(0, 58);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(559, 319);
            this.groupBox1.TabIndex = 12;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Zero-K lobby settings: (changes need restart)";
            // 
            // propertyGrid1
            // 
            this.propertyGrid1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.propertyGrid1.Location = new System.Drawing.Point(3, 16);
            this.propertyGrid1.Name = "propertyGrid1";
            this.propertyGrid1.Size = new System.Drawing.Size(553, 300);
            this.propertyGrid1.TabIndex = 0;
            // 
            // btnKeybindings
            // 
            this.btnKeybindings.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.btnKeybindings.Location = new System.Drawing.Point(126, 3);
            this.btnKeybindings.Name = "btnKeybindings";
            this.btnKeybindings.Size = new System.Drawing.Size(87, 49);
            this.btnKeybindings.TabIndex = 14;
            this.btnKeybindings.Text = "Keyboard settings";
            this.btnKeybindings.UseVisualStyleBackColor = true;
            this.btnKeybindings.Click += new System.EventHandler(this.btnKeybindings_Click);
            // 
            // btnDisplay
            // 
            this.btnDisplay.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.btnDisplay.Location = new System.Drawing.Point(3, 3);
            this.btnDisplay.Name = "btnDisplay";
            this.btnDisplay.Size = new System.Drawing.Size(89, 49);
            this.btnDisplay.TabIndex = 13;
            this.btnDisplay.Text = "Graphics settings";
            this.btnDisplay.UseVisualStyleBackColor = true;
            this.btnDisplay.Click += new System.EventHandler(this.btnDisplay_Click);
            // 
            // SettingsTab
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.btnKeybindings);
            this.Controls.Add(this.btnDisplay);
            this.Controls.Add(this.groupBox1);
            this.Name = "SettingsTab";
            this.Size = new System.Drawing.Size(562, 380);
            this.Load += new System.EventHandler(this.SettingsTab_Load);
            this.groupBox1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.PropertyGrid propertyGrid1;
        private System.Windows.Forms.Button btnKeybindings;
        private System.Windows.Forms.Button btnDisplay;
    }
}
