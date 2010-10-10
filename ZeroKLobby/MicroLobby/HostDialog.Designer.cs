namespace SpringDownloader.MicroLobby
{
    partial class HostDialog
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
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.battleTitleBox = new System.Windows.Forms.TextBox();
            this.passwordBox = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.okButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.teamsBar = new System.Windows.Forms.TrackBar();
            this.maxPlayersBar = new System.Windows.Forms.TrackBar();
            this.minPlayersBar = new System.Windows.Forms.TrackBar();
            this.maxPlayersLabel = new System.Windows.Forms.Label();
            this.minPlayersLabel = new System.Windows.Forms.Label();
            this.teamsLabel = new System.Windows.Forms.Label();
            this.enableManageBox = new System.Windows.Forms.CheckBox();
            this.gameBox = new System.Windows.Forms.ComboBox();
            this.showAdvancedButton = new System.Windows.Forms.Button();
            this.advancedOptionsGroup = new System.Windows.Forms.GroupBox();
            this.springieCommandsBox = new System.Windows.Forms.TextBox();
            this.rapidTagBox = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.teamsBar)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.maxPlayersBar)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.minPlayersBar)).BeginInit();
            this.advancedOptionsGroup.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(16, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(85, 17);
            this.label1.TabIndex = 0;
            this.label1.Text = "Battle Name";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(16, 41);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(69, 17);
            this.label2.TabIndex = 1;
            this.label2.Text = "Password";
            // 
            // battleTitleBox
            // 
            this.battleTitleBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.battleTitleBox.Location = new System.Drawing.Point(148, 12);
            this.battleTitleBox.Name = "battleTitleBox";
            this.battleTitleBox.Size = new System.Drawing.Size(380, 20);
            this.battleTitleBox.TabIndex = 3;
            // 
            // passwordBox
            // 
            this.passwordBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.passwordBox.Location = new System.Drawing.Point(148, 38);
            this.passwordBox.Name = "passwordBox";
            this.passwordBox.Size = new System.Drawing.Size(380, 20);
            this.passwordBox.TabIndex = 4;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.Location = new System.Drawing.Point(19, 69);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(46, 17);
            this.label4.TabIndex = 6;
            this.label4.Text = "Game";
            // 
            // okButton
            // 
            this.okButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.okButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.okButton.Location = new System.Drawing.Point(295, 531);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(118, 29);
            this.okButton.TabIndex = 8;
            this.okButton.Text = "OK";
            this.okButton.UseVisualStyleBackColor = true;
            this.okButton.Click += new System.EventHandler(this.okButton_Click);
            // 
            // cancelButton
            // 
            this.cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.Location = new System.Drawing.Point(419, 531);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(109, 29);
            this.cancelButton.TabIndex = 9;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.teamsBar);
            this.groupBox1.Controls.Add(this.maxPlayersBar);
            this.groupBox1.Controls.Add(this.minPlayersBar);
            this.groupBox1.Controls.Add(this.maxPlayersLabel);
            this.groupBox1.Controls.Add(this.minPlayersLabel);
            this.groupBox1.Controls.Add(this.teamsLabel);
            this.groupBox1.Controls.Add(this.enableManageBox);
            this.groupBox1.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.groupBox1.Location = new System.Drawing.Point(13, 102);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(515, 229);
            this.groupBox1.TabIndex = 10;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Automatically Manage Battle";
            // 
            // teamsBar
            // 
            this.teamsBar.Location = new System.Drawing.Point(156, 181);
            this.teamsBar.Maximum = 16;
            this.teamsBar.Minimum = 2;
            this.teamsBar.Name = "teamsBar";
            this.teamsBar.Size = new System.Drawing.Size(353, 45);
            this.teamsBar.TabIndex = 7;
            this.teamsBar.Value = 2;
            // 
            // maxPlayersBar
            // 
            this.maxPlayersBar.Location = new System.Drawing.Point(156, 130);
            this.maxPlayersBar.Maximum = 16;
            this.maxPlayersBar.Minimum = 2;
            this.maxPlayersBar.Name = "maxPlayersBar";
            this.maxPlayersBar.Size = new System.Drawing.Size(353, 45);
            this.maxPlayersBar.TabIndex = 6;
            this.maxPlayersBar.Value = 16;
            // 
            // minPlayersBar
            // 
            this.minPlayersBar.Location = new System.Drawing.Point(156, 79);
            this.minPlayersBar.Maximum = 16;
            this.minPlayersBar.Minimum = 1;
            this.minPlayersBar.Name = "minPlayersBar";
            this.minPlayersBar.Size = new System.Drawing.Size(353, 45);
            this.minPlayersBar.TabIndex = 5;
            this.minPlayersBar.Value = 8;
            // 
            // maxPlayersLabel
            // 
            this.maxPlayersLabel.AutoSize = true;
            this.maxPlayersLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.maxPlayersLabel.Location = new System.Drawing.Point(3, 130);
            this.maxPlayersLabel.Name = "maxPlayersLabel";
            this.maxPlayersLabel.Size = new System.Drawing.Size(147, 17);
            this.maxPlayersLabel.TabIndex = 4;
            this.maxPlayersLabel.Text = "Maximum Players (16)";
            // 
            // minPlayersLabel
            // 
            this.minPlayersLabel.AutoSize = true;
            this.minPlayersLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.minPlayersLabel.Location = new System.Drawing.Point(6, 79);
            this.minPlayersLabel.Name = "minPlayersLabel";
            this.minPlayersLabel.Size = new System.Drawing.Size(136, 17);
            this.minPlayersLabel.TabIndex = 3;
            this.minPlayersLabel.Text = "Minimum Players (6)";
            // 
            // teamsLabel
            // 
            this.teamsLabel.AutoSize = true;
            this.teamsLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.teamsLabel.Location = new System.Drawing.Point(6, 181);
            this.teamsLabel.Name = "teamsLabel";
            this.teamsLabel.Size = new System.Drawing.Size(73, 17);
            this.teamsLabel.TabIndex = 2;
            this.teamsLabel.Text = "Teams (2)";
            // 
            // enableManageBox
            // 
            this.enableManageBox.AutoSize = true;
            this.enableManageBox.Checked = true;
            this.enableManageBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.enableManageBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.enableManageBox.Location = new System.Drawing.Point(9, 41);
            this.enableManageBox.Name = "enableManageBox";
            this.enableManageBox.Size = new System.Drawing.Size(71, 21);
            this.enableManageBox.TabIndex = 1;
            this.enableManageBox.Text = "Enable";
            this.enableManageBox.UseVisualStyleBackColor = true;
            this.enableManageBox.CheckedChanged += new System.EventHandler(this.enableManageBox_CheckedChanged);
            // 
            // gameBox
            // 
            this.gameBox.Location = new System.Drawing.Point(148, 64);
            this.gameBox.Name = "gameBox";
            this.gameBox.Size = new System.Drawing.Size(380, 21);
            this.gameBox.TabIndex = 11;
            // 
            // showAdvancedButton
            // 
            this.showAdvancedButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.showAdvancedButton.Location = new System.Drawing.Point(13, 534);
            this.showAdvancedButton.Name = "showAdvancedButton";
            this.showAdvancedButton.Size = new System.Drawing.Size(133, 23);
            this.showAdvancedButton.TabIndex = 12;
            this.showAdvancedButton.Text = "Show Advanced Options";
            this.showAdvancedButton.UseVisualStyleBackColor = true;
            this.showAdvancedButton.Click += new System.EventHandler(this.showAdvancedButton_Click);
            // 
            // advancedOptionsGroup
            // 
            this.advancedOptionsGroup.Controls.Add(this.springieCommandsBox);
            this.advancedOptionsGroup.Controls.Add(this.rapidTagBox);
            this.advancedOptionsGroup.Controls.Add(this.label5);
            this.advancedOptionsGroup.Controls.Add(this.label3);
            this.advancedOptionsGroup.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.advancedOptionsGroup.Location = new System.Drawing.Point(12, 338);
            this.advancedOptionsGroup.Name = "advancedOptionsGroup";
            this.advancedOptionsGroup.Size = new System.Drawing.Size(516, 181);
            this.advancedOptionsGroup.TabIndex = 13;
            this.advancedOptionsGroup.TabStop = false;
            this.advancedOptionsGroup.Text = "Advanced Options";
            // 
            // springieCommandsBox
            // 
            this.springieCommandsBox.AcceptsReturn = true;
            this.springieCommandsBox.Location = new System.Drawing.Point(157, 58);
            this.springieCommandsBox.Multiline = true;
            this.springieCommandsBox.Name = "springieCommandsBox";
            this.springieCommandsBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.springieCommandsBox.Size = new System.Drawing.Size(353, 117);
            this.springieCommandsBox.TabIndex = 3;
            // 
            // rapidTagBox
            // 
            this.rapidTagBox.Location = new System.Drawing.Point(157, 27);
            this.rapidTagBox.Name = "rapidTagBox";
            this.rapidTagBox.Size = new System.Drawing.Size(353, 22);
            this.rapidTagBox.TabIndex = 2;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.Location = new System.Drawing.Point(10, 63);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(134, 17);
            this.label5.TabIndex = 1;
            this.label5.Text = "Springie Commands";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(10, 32);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(87, 17);
            this.label3.TabIndex = 0;
            this.label3.Text = "Game Name";
            // 
            // HostDialog
            // 
            this.AcceptButton = this.okButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.cancelButton;
            this.ClientSize = new System.Drawing.Size(540, 566);
            this.Controls.Add(this.advancedOptionsGroup);
            this.Controls.Add(this.showAdvancedButton);
            this.Controls.Add(this.gameBox);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.okButton);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.passwordBox);
            this.Controls.Add(this.battleTitleBox);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "HostDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.teamsBar)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.maxPlayersBar)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.minPlayersBar)).EndInit();
            this.advancedOptionsGroup.ResumeLayout(false);
            this.advancedOptionsGroup.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.ComboBox gameBox;
        private System.Windows.Forms.Label maxPlayersLabel;
        private System.Windows.Forms.Label minPlayersLabel;
        private System.Windows.Forms.Label teamsLabel;
        private System.Windows.Forms.CheckBox enableManageBox;
        private System.Windows.Forms.TrackBar teamsBar;
        private System.Windows.Forms.TrackBar maxPlayersBar;
        private System.Windows.Forms.TrackBar minPlayersBar;
        private System.Windows.Forms.Button showAdvancedButton;
        private System.Windows.Forms.GroupBox advancedOptionsGroup;
        private System.Windows.Forms.TextBox springieCommandsBox;
        private System.Windows.Forms.TextBox rapidTagBox;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox battleTitleBox;
        private System.Windows.Forms.TextBox passwordBox;
    }
}
