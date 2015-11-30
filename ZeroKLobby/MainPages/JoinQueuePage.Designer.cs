namespace ZeroKLobby.MainPages
{
    partial class JoinQueuePage
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
            this.JoinQueueButton = new ZeroKLobby.BitmapButton();
            this.PartyPlayerList = new ZeroKLobby.MicroLobby.PlayerListBox();
            this.PartyBoxTitleLabel = new System.Windows.Forms.Label();
            this.OneVsOneCheckBox = new System.Windows.Forms.CheckBox();
            this.TeamsCheckBox = new System.Windows.Forms.CheckBox();
            this.InvitePartyMemberButton = new ZeroKLobby.BitmapButton();
            this.LeavePartyButton = new ZeroKLobby.BitmapButton();
            this.SuspendLayout();
            // 
            // JoinQueueButton
            // 
            this.JoinQueueButton.BackColor = System.Drawing.Color.Transparent;
            this.JoinQueueButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.JoinQueueButton.ButtonStyle = ZeroKLobby.FrameBorderRenderer.StyleType.DarkHive;
            this.JoinQueueButton.Cursor = System.Windows.Forms.Cursors.Hand;
            this.JoinQueueButton.FlatAppearance.BorderSize = 0;
            this.JoinQueueButton.FlatAppearance.CheckedBackColor = System.Drawing.Color.Transparent;
            this.JoinQueueButton.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Transparent;
            this.JoinQueueButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Transparent;
            this.JoinQueueButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.JoinQueueButton.ForeColor = System.Drawing.Color.White;
            this.JoinQueueButton.Location = new System.Drawing.Point(475, 105);
            this.JoinQueueButton.Name = "JoinQueueButton";
            this.JoinQueueButton.Size = new System.Drawing.Size(237, 83);
            this.JoinQueueButton.SoundType = ZeroKLobby.Controls.SoundPalette.SoundType.Click;
            this.JoinQueueButton.TabIndex = 0;
            this.JoinQueueButton.Text = "Join Queue";
            this.JoinQueueButton.UseVisualStyleBackColor = false;
            this.JoinQueueButton.Click += new System.EventHandler(this.JoinQueueButton_Click);
            // 
            // PartyPlayerList
            // 
            this.PartyPlayerList.BackColor = System.Drawing.Color.DimGray;
            this.PartyPlayerList.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawVariable;
            this.PartyPlayerList.FormattingEnabled = true;
            this.PartyPlayerList.HoverItem = null;
            this.PartyPlayerList.IntegralHeight = false;
            this.PartyPlayerList.IsBattle = false;
            this.PartyPlayerList.ItemHeight = 10;
            this.PartyPlayerList.Location = new System.Drawing.Point(58, 91);
            this.PartyPlayerList.Name = "PartyPlayerList";
            this.PartyPlayerList.Size = new System.Drawing.Size(310, 372);
            this.PartyPlayerList.TabIndex = 1;
            // 
            // PartyBoxTitleLabel
            // 
            this.PartyBoxTitleLabel.AutoSize = true;
            this.PartyBoxTitleLabel.Location = new System.Drawing.Point(177, 55);
            this.PartyBoxTitleLabel.Name = "PartyBoxTitleLabel";
            this.PartyBoxTitleLabel.Size = new System.Drawing.Size(77, 13);
            this.PartyBoxTitleLabel.TabIndex = 2;
            this.PartyBoxTitleLabel.Text = "Party Members";
            this.PartyBoxTitleLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // OneVsOneCheckBox
            // 
            this.OneVsOneCheckBox.AutoSize = true;
            this.OneVsOneCheckBox.Location = new System.Drawing.Point(576, 227);
            this.OneVsOneCheckBox.Name = "OneVsOneCheckBox";
            this.OneVsOneCheckBox.Size = new System.Drawing.Size(44, 17);
            this.OneVsOneCheckBox.TabIndex = 3;
            this.OneVsOneCheckBox.Text = "1v1";
            this.OneVsOneCheckBox.UseVisualStyleBackColor = true;
            // 
            // TeamsCheckBox
            // 
            this.TeamsCheckBox.AutoSize = true;
            this.TeamsCheckBox.Location = new System.Drawing.Point(576, 305);
            this.TeamsCheckBox.Name = "TeamsCheckBox";
            this.TeamsCheckBox.Size = new System.Drawing.Size(58, 17);
            this.TeamsCheckBox.TabIndex = 4;
            this.TeamsCheckBox.Text = "Teams";
            this.TeamsCheckBox.UseVisualStyleBackColor = true;
            // 
            // InvitePartyMemberButton
            // 
            this.InvitePartyMemberButton.BackColor = System.Drawing.Color.Transparent;
            this.InvitePartyMemberButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.InvitePartyMemberButton.ButtonStyle = ZeroKLobby.FrameBorderRenderer.StyleType.DarkHive;
            this.InvitePartyMemberButton.Cursor = System.Windows.Forms.Cursors.Hand;
            this.InvitePartyMemberButton.FlatAppearance.BorderSize = 0;
            this.InvitePartyMemberButton.FlatAppearance.CheckedBackColor = System.Drawing.Color.Transparent;
            this.InvitePartyMemberButton.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Transparent;
            this.InvitePartyMemberButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Transparent;
            this.InvitePartyMemberButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.InvitePartyMemberButton.ForeColor = System.Drawing.Color.White;
            this.InvitePartyMemberButton.Location = new System.Drawing.Point(60, 481);
            this.InvitePartyMemberButton.Name = "InvitePartyMemberButton";
            this.InvitePartyMemberButton.Size = new System.Drawing.Size(249, 59);
            this.InvitePartyMemberButton.SoundType = ZeroKLobby.Controls.SoundPalette.SoundType.Click;
            this.InvitePartyMemberButton.TabIndex = 5;
            this.InvitePartyMemberButton.Text = "Invite Friend";
            this.InvitePartyMemberButton.UseVisualStyleBackColor = false;
            this.InvitePartyMemberButton.Click += new System.EventHandler(this.InvitePartyMemberButton_Click);
            // 
            // LeavePartyButton
            // 
            this.LeavePartyButton.BackColor = System.Drawing.Color.Transparent;
            this.LeavePartyButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.LeavePartyButton.ButtonStyle = ZeroKLobby.FrameBorderRenderer.StyleType.DarkHive;
            this.LeavePartyButton.Cursor = System.Windows.Forms.Cursors.Hand;
            this.LeavePartyButton.FlatAppearance.BorderSize = 0;
            this.LeavePartyButton.FlatAppearance.CheckedBackColor = System.Drawing.Color.Transparent;
            this.LeavePartyButton.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Transparent;
            this.LeavePartyButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Transparent;
            this.LeavePartyButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.LeavePartyButton.ForeColor = System.Drawing.Color.White;
            this.LeavePartyButton.Location = new System.Drawing.Point(58, 546);
            this.LeavePartyButton.Name = "LeavePartyButton";
            this.LeavePartyButton.Size = new System.Drawing.Size(251, 51);
            this.LeavePartyButton.SoundType = ZeroKLobby.Controls.SoundPalette.SoundType.Click;
            this.LeavePartyButton.TabIndex = 6;
            this.LeavePartyButton.Text = "Leave Party";
            this.LeavePartyButton.UseVisualStyleBackColor = false;
            this.LeavePartyButton.Click += new System.EventHandler(this.LeavePartyButton_Click);
            // 
            // JoinQueuePage
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Transparent;
            this.Controls.Add(this.LeavePartyButton);
            this.Controls.Add(this.InvitePartyMemberButton);
            this.Controls.Add(this.TeamsCheckBox);
            this.Controls.Add(this.OneVsOneCheckBox);
            this.Controls.Add(this.PartyBoxTitleLabel);
            this.Controls.Add(this.PartyPlayerList);
            this.Controls.Add(this.JoinQueueButton);
            this.Name = "JoinQueuePage";
            this.Size = new System.Drawing.Size(1080, 701);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private BitmapButton JoinQueueButton;
        private MicroLobby.PlayerListBox PartyPlayerList;
        private System.Windows.Forms.Label PartyBoxTitleLabel;
        private System.Windows.Forms.CheckBox OneVsOneCheckBox;
        private System.Windows.Forms.CheckBox TeamsCheckBox;
        private BitmapButton InvitePartyMemberButton;
        private BitmapButton LeavePartyButton;
    }
}
