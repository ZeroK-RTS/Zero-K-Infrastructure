using ZeroKLobby.MicroLobby;

namespace ZeroKLobby.Notifications
{
    partial class BattleBar
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
					this.cbSide = new System.Windows.Forms.ComboBox();
					this.numMinValue = new System.Windows.Forms.NumericUpDown();
					this.cbSpectate = new System.Windows.Forms.CheckBox();
					this.lbPlayers = new System.Windows.Forms.Label();
					this.lbGameName = new System.Windows.Forms.Label();
					this.lbSide = new System.Windows.Forms.Label();
					this.lbMin = new System.Windows.Forms.Label();
					this.gameBox = new System.Windows.Forms.PictureBox();
					this.picoChat = new ChatBox();
					((System.ComponentModel.ISupportInitialize)(this.numMinValue)).BeginInit();
					((System.ComponentModel.ISupportInitialize)(this.gameBox)).BeginInit();
					this.SuspendLayout();
					// 
					// cbSide
					// 
					this.cbSide.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
					this.cbSide.FormattingEnabled = true;
					this.cbSide.Location = new System.Drawing.Point(54, 52);
					this.cbSide.Name = "cbSide";
					this.cbSide.Size = new System.Drawing.Size(114, 21);
					this.cbSide.TabIndex = 4;
					this.cbSide.Visible = false;
					this.cbSide.SelectedIndexChanged += new System.EventHandler(this.cbSide_SelectedIndexChanged);
					this.cbSide.VisibleChanged += new System.EventHandler(this.cbSide_VisibleChanged);
					// 
					// numMinValue
					// 
					this.numMinValue.Location = new System.Drawing.Point(124, 28);
					this.numMinValue.Maximum = new decimal(new int[] {
            10,
            0,
            0,
            0});
					this.numMinValue.Name = "numMinValue";
					this.numMinValue.Size = new System.Drawing.Size(44, 20);
					this.numMinValue.TabIndex = 5;
					this.numMinValue.Value = new decimal(new int[] {
            2,
            0,
            0,
            0});
					this.numMinValue.ValueChanged += new System.EventHandler(this.numMinValue_ValueChanged);
					// 
					// cbSpectate
					// 
					this.cbSpectate.AutoSize = true;
					this.cbSpectate.Location = new System.Drawing.Point(10, 29);
					this.cbSpectate.Name = "cbSpectate";
					this.cbSpectate.Size = new System.Drawing.Size(69, 17);
					this.cbSpectate.TabIndex = 10;
					this.cbSpectate.Text = "Spectate";
					this.cbSpectate.UseVisualStyleBackColor = true;
					this.cbSpectate.CheckedChanged += new System.EventHandler(this.cbSpectate_CheckedChanged);
					// 
					// lbPlayers
					// 
					this.lbPlayers.AutoSize = true;
					this.lbPlayers.Location = new System.Drawing.Point(68, 5);
					this.lbPlayers.Name = "lbPlayers";
					this.lbPlayers.Size = new System.Drawing.Size(0, 13);
					this.lbPlayers.TabIndex = 3;
					// 
					// lbGameName
					// 
					this.lbGameName.AutoSize = true;
					this.lbGameName.Location = new System.Drawing.Point(7, 5);
					this.lbGameName.Name = "lbGameName";
					this.lbGameName.Size = new System.Drawing.Size(61, 13);
					this.lbGameName.TabIndex = 6;
					this.lbGameName.Text = "Connecting";
					// 
					// lbSide
					// 
					this.lbSide.AutoSize = true;
					this.lbSide.Location = new System.Drawing.Point(3, 55);
					this.lbSide.Name = "lbSide";
					this.lbSide.Size = new System.Drawing.Size(45, 13);
					this.lbSide.TabIndex = 7;
					this.lbSide.Text = "Faction:";
					// 
					// lbMin
					// 
					this.lbMin.AutoSize = true;
					this.lbMin.Location = new System.Drawing.Point(85, 30);
					this.lbMin.Name = "lbMin";
					this.lbMin.Size = new System.Drawing.Size(33, 13);
					this.lbMin.TabIndex = 8;
					this.lbMin.Text = "Min. :";
					// 
					// gameBox
					// 
					this.gameBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
					this.gameBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
					this.gameBox.Location = new System.Drawing.Point(582, 0);
					this.gameBox.Name = "gameBox";
					this.gameBox.Size = new System.Drawing.Size(306, 76);
					this.gameBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
					this.gameBox.TabIndex = 11;
					this.gameBox.TabStop = false;
					// 
					// picoChat
					// 
					this.picoChat.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
											| System.Windows.Forms.AnchorStyles.Left)
											| System.Windows.Forms.AnchorStyles.Right)));
					this.picoChat.BackColor = System.Drawing.Color.White;
					this.picoChat.ChatBackgroundColor = 0;
					this.picoChat.Font = new System.Drawing.Font("Verdana", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
					this.picoChat.HideScroll = false;
					this.picoChat.IRCForeColor = 0;
					this.picoChat.Location = new System.Drawing.Point(181, 5);
					this.picoChat.Name = "picoChat";
					this.picoChat.NoColorMode = false;
					this.picoChat.ShowHistory = true;
					this.picoChat.ShowJoinLeave = false;
					this.picoChat.ShowUnreadLine = true;
					this.picoChat.SingleLine = false;
					this.picoChat.Size = new System.Drawing.Size(392, 63);
					this.picoChat.TabIndex = 12;
					this.picoChat.TextFilter = null;
					this.picoChat.TotalDisplayLines = 0;
					this.picoChat.UseTopicBackground = false;
					// 
					// BattleBar
					// 
					this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
					this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
					this.Controls.Add(this.picoChat);
					this.Controls.Add(this.gameBox);
					this.Controls.Add(this.cbSpectate);
					this.Controls.Add(this.lbMin);
					this.Controls.Add(this.lbSide);
					this.Controls.Add(this.lbGameName);
					this.Controls.Add(this.lbPlayers);
					this.Controls.Add(this.numMinValue);
					this.Controls.Add(this.cbSide);
					this.MinimumSize = new System.Drawing.Size(492, 76);
					this.Name = "BattleBar";
					this.Size = new System.Drawing.Size(888, 76);
					this.Load += new System.EventHandler(this.QuickMatchControl_Load);
					((System.ComponentModel.ISupportInitialize)(this.numMinValue)).EndInit();
					((System.ComponentModel.ISupportInitialize)(this.gameBox)).EndInit();
					this.ResumeLayout(false);
					this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox cbSide;
        private System.Windows.Forms.NumericUpDown numMinValue;
        private System.Windows.Forms.Label lbPlayers;
        private System.Windows.Forms.Label lbGameName;
        private System.Windows.Forms.Label lbSide;
        private System.Windows.Forms.Label lbMin;
        private System.Windows.Forms.CheckBox cbSpectate;
        private System.Windows.Forms.PictureBox gameBox;
        private ChatBox picoChat;
    }
}