namespace ZeroKLobby.Controls
{
    partial class MatchQueueControl
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
            this.DescriptionLabel = new System.Windows.Forms.Label();
            this.TimerLabel = new System.Windows.Forms.Label();
            this.QueueNumbersLabel = new System.Windows.Forms.Label();
            this.LeaveQueueButton = new ZeroKLobby.BitmapButton();
            this.SuspendLayout();
            // 
            // DescriptionLabel
            // 
            this.DescriptionLabel.AutoSize = true;
            this.DescriptionLabel.Location = new System.Drawing.Point(3, 31);
            this.DescriptionLabel.Name = "DescriptionLabel";
            this.DescriptionLabel.Size = new System.Drawing.Size(136, 13);
            this.DescriptionLabel.TabIndex = 0;
            this.DescriptionLabel.Text = "You Are in Queue for Battle";
            // 
            // TimerLabel
            // 
            this.TimerLabel.AutoSize = true;
            this.TimerLabel.Location = new System.Drawing.Point(47, 58);
            this.TimerLabel.Name = "TimerLabel";
            this.TimerLabel.Size = new System.Drawing.Size(34, 13);
            this.TimerLabel.TabIndex = 1;
            this.TimerLabel.Text = "02:12";
            // 
            // QueueNumbersLabel
            // 
            this.QueueNumbersLabel.AutoSize = true;
            this.QueueNumbersLabel.Location = new System.Drawing.Point(446, 31);
            this.QueueNumbersLabel.Name = "QueueNumbersLabel";
            this.QueueNumbersLabel.Size = new System.Drawing.Size(151, 26);
            this.QueueNumbersLabel.TabIndex = 2;
            this.QueueNumbersLabel.Text = "People in Queue\r\n1v1: 3 Teams: 5 Planetwars: 3";
            this.QueueNumbersLabel.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // LeaveQueueButton
            // 
            this.LeaveQueueButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.LeaveQueueButton.BackColor = System.Drawing.Color.Transparent;
            this.LeaveQueueButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.LeaveQueueButton.ButtonStyle = ZeroKLobby.FrameBorderRenderer.StyleType.DarkHive;
            this.LeaveQueueButton.Cursor = System.Windows.Forms.Cursors.Hand;
            this.LeaveQueueButton.FlatAppearance.BorderSize = 0;
            this.LeaveQueueButton.FlatAppearance.CheckedBackColor = System.Drawing.Color.Transparent;
            this.LeaveQueueButton.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Transparent;
            this.LeaveQueueButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Transparent;
            this.LeaveQueueButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.LeaveQueueButton.ForeColor = System.Drawing.Color.White;
            this.LeaveQueueButton.Location = new System.Drawing.Point(776, 31);
            this.LeaveQueueButton.Name = "LeaveQueueButton";
            this.LeaveQueueButton.Size = new System.Drawing.Size(203, 49);
            this.LeaveQueueButton.SoundType = ZeroKLobby.Controls.SoundPalette.SoundType.Click;
            this.LeaveQueueButton.TabIndex = 4;
            this.LeaveQueueButton.Text = "Leave Queue";
            this.LeaveQueueButton.UseVisualStyleBackColor = false;
            // 
            // MatchQueueControl
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.Transparent;
            this.Controls.Add(this.LeaveQueueButton);
            this.Controls.Add(this.QueueNumbersLabel);
            this.Controls.Add(this.TimerLabel);
            this.Controls.Add(this.DescriptionLabel);
            this.DoubleBuffered = true;
            this.Name = "MatchQueueControl";
            this.Size = new System.Drawing.Size(1020, 112);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label DescriptionLabel;
        private System.Windows.Forms.Label TimerLabel;
        private System.Windows.Forms.Label QueueNumbersLabel;
        private BitmapButton LeaveQueueButton;
    }
}
