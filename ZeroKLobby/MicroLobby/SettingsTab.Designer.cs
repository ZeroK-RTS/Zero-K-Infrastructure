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
          this.panel1 = new System.Windows.Forms.Panel();
          this.feedbackButton = new System.Windows.Forms.Button();
          this.logButton = new System.Windows.Forms.Button();
          this.helpButton = new System.Windows.Forms.Button();
          this.problemButton = new System.Windows.Forms.Button();
          this.groupBox1.SuspendLayout();
          this.panel1.SuspendLayout();
          this.SuspendLayout();
          // 
          // groupBox1
          // 
          this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                      | System.Windows.Forms.AnchorStyles.Left)
                      | System.Windows.Forms.AnchorStyles.Right)));
          this.groupBox1.Controls.Add(this.propertyGrid1);
          this.groupBox1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
          this.groupBox1.Location = new System.Drawing.Point(0, 108);
          this.groupBox1.Name = "groupBox1";
          this.groupBox1.Size = new System.Drawing.Size(559, 269);
          this.groupBox1.TabIndex = 12;
          this.groupBox1.TabStop = false;
          this.groupBox1.Text = "Zero-K lobby settings: (changes need restart)";
          // 
          // propertyGrid1
          // 
          this.propertyGrid1.Dock = System.Windows.Forms.DockStyle.Fill;
          this.propertyGrid1.Location = new System.Drawing.Point(3, 16);
          this.propertyGrid1.Name = "propertyGrid1";
          this.propertyGrid1.Size = new System.Drawing.Size(553, 250);
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
          // panel1
          // 
          this.panel1.Controls.Add(this.feedbackButton);
          this.panel1.Controls.Add(this.logButton);
          this.panel1.Controls.Add(this.helpButton);
          this.panel1.Controls.Add(this.problemButton);
          this.panel1.Controls.Add(this.groupBox1);
          this.panel1.Controls.Add(this.btnDisplay);
          this.panel1.Controls.Add(this.btnKeybindings);
          this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
          this.panel1.Location = new System.Drawing.Point(0, 0);
          this.panel1.Name = "panel1";
          this.panel1.Size = new System.Drawing.Size(562, 380);
          this.panel1.TabIndex = 15;
          // 
          // feedbackButton
          // 
          this.feedbackButton.Location = new System.Drawing.Point(241, 67);
          this.feedbackButton.Name = "feedbackButton";
          this.feedbackButton.Size = new System.Drawing.Size(211, 23);
          this.feedbackButton.TabIndex = 16;
          this.feedbackButton.Text = "Tell us what you\'d like to see in Zero-K";
          this.feedbackButton.UseVisualStyleBackColor = true;
          // 
          // logButton
          // 
          this.logButton.Location = new System.Drawing.Point(241, 29);
          this.logButton.Name = "logButton";
          this.logButton.Size = new System.Drawing.Size(124, 23);
          this.logButton.TabIndex = 18;
          this.logButton.Text = "Show Diagnostic Log";
          this.logButton.UseVisualStyleBackColor = true;
          this.logButton.Click += new System.EventHandler(this.logButton_Click);
          // 
          // helpButton
          // 
          this.helpButton.Location = new System.Drawing.Point(127, 67);
          this.helpButton.Name = "helpButton";
          this.helpButton.Size = new System.Drawing.Size(86, 23);
          this.helpButton.TabIndex = 15;
          this.helpButton.Text = "Ask for Help";
          this.helpButton.UseVisualStyleBackColor = true;
          // 
          // problemButton
          // 
          this.problemButton.Location = new System.Drawing.Point(3, 67);
          this.problemButton.Name = "problemButton";
          this.problemButton.Size = new System.Drawing.Size(103, 23);
          this.problemButton.TabIndex = 17;
          this.problemButton.Text = "Report a Problem";
          this.problemButton.UseVisualStyleBackColor = true;
          this.problemButton.Click += new System.EventHandler(this.problemButton_Click);
          // 
          // SettingsTab
          // 
          this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
          this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
          this.Controls.Add(this.panel1);
          this.Name = "SettingsTab";
          this.Size = new System.Drawing.Size(562, 380);
          this.Load += new System.EventHandler(this.SettingsTab_Load);
          this.groupBox1.ResumeLayout(false);
          this.panel1.ResumeLayout(false);
          this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.PropertyGrid propertyGrid1;
        private System.Windows.Forms.Button btnKeybindings;
        private System.Windows.Forms.Button btnDisplay;
		private System.Windows.Forms.Panel panel1;
    private System.Windows.Forms.Button feedbackButton;
    private System.Windows.Forms.Button logButton;
    private System.Windows.Forms.Button helpButton;
    private System.Windows.Forms.Button problemButton;
    }
}
