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
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(BattleBar));
			this.cbSide = new System.Windows.Forms.ComboBox();
			this.lbPlayers = new System.Windows.Forms.Label();
			this.gameBox = new System.Windows.Forms.PictureBox();
			this.cbReady = new System.Windows.Forms.CheckBox();
			this.imageList1 = new System.Windows.Forms.ImageList(this.components);
			this.panel1 = new System.Windows.Forms.Panel();
			this.battleExtras = new System.Windows.Forms.Button();
			this.picoChat = new ZeroKLobby.MicroLobby.ChatBox();
			((System.ComponentModel.ISupportInitialize)(this.gameBox)).BeginInit();
			this.panel1.SuspendLayout();
			this.SuspendLayout();
			// 
			// cbSide
			// 
			this.cbSide.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cbSide.FormattingEnabled = true;
			this.cbSide.Location = new System.Drawing.Point(10, 21);
			this.cbSide.Name = "cbSide";
			this.cbSide.Size = new System.Drawing.Size(114, 21);
			this.cbSide.TabIndex = 4;
			this.cbSide.Visible = false;
			this.cbSide.SelectedIndexChanged += new System.EventHandler(this.cbSide_SelectedIndexChanged);
			// 
			// lbPlayers
			// 
			this.lbPlayers.AutoSize = true;
			this.lbPlayers.Location = new System.Drawing.Point(68, 5);
			this.lbPlayers.Name = "lbPlayers";
			this.lbPlayers.Size = new System.Drawing.Size(0, 13);
			this.lbPlayers.TabIndex = 3;
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
			// cbReady
			// 
			this.cbReady.AutoSize = true;
			this.cbReady.ImageIndex = 2;
			this.cbReady.ImageList = this.imageList1;
			this.cbReady.Location = new System.Drawing.Point(10, 6);
			this.cbReady.Name = "cbReady";
			this.cbReady.Size = new System.Drawing.Size(73, 17);
			this.cbReady.TabIndex = 13;
			this.cbReady.Text = "Ready";
			this.cbReady.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
			this.cbReady.UseVisualStyleBackColor = true;
			this.cbReady.CheckedChanged += new System.EventHandler(this.cbReady_CheckedChanged);
			this.cbReady.Click += new System.EventHandler(this.cbReady_Click);
			// 
			// imageList1
			// 
			this.imageList1.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList1.ImageStream")));
			this.imageList1.TransparentColor = System.Drawing.Color.Transparent;
			this.imageList1.Images.SetKeyName(0, "joined.ico");
			this.imageList1.Images.SetKeyName(1, "ok.ico");
			this.imageList1.Images.SetKeyName(2, "run.ico");
			// 
			// panel1
			// 
			this.panel1.Controls.Add(this.battleExtras);
			this.panel1.Controls.Add(this.cbSide);
			this.panel1.Controls.Add(this.lbPlayers);
			this.panel1.Controls.Add(this.gameBox);
			this.panel1.Controls.Add(this.picoChat);
			this.panel1.Controls.Add(this.cbReady);
			this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panel1.Location = new System.Drawing.Point(0, 0);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(888, 76);
			this.panel1.TabIndex = 15;
			// 
			// battleExtras
			// 
			this.battleExtras.Location = new System.Drawing.Point(10, 42);
			this.battleExtras.Name = "battleExtras";
			this.battleExtras.Size = new System.Drawing.Size(58, 23);
			this.battleExtras.TabIndex = 14;
			this.battleExtras.Text = "Extras";
			this.battleExtras.UseVisualStyleBackColor = true;
			this.battleExtras.Click += new System.EventHandler(this.battleExtras_Click);
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
			this.picoChat.Location = new System.Drawing.Point(130, 5);
			this.picoChat.Name = "picoChat";
			this.picoChat.NoColorMode = false;
			this.picoChat.ShowHistory = true;
			this.picoChat.ShowJoinLeave = false;
			this.picoChat.ShowUnreadLine = true;
			this.picoChat.SingleLine = false;
			this.picoChat.Size = new System.Drawing.Size(443, 63);
			this.picoChat.TabIndex = 12;
			this.picoChat.TextFilter = null;
			this.picoChat.TotalDisplayLines = 0;
			this.picoChat.UseTopicBackground = false;
			// 
			// BattleBar
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.panel1);
			this.MinimumSize = new System.Drawing.Size(492, 76);
			this.Name = "BattleBar";
			this.Size = new System.Drawing.Size(888, 76);
			this.Load += new System.EventHandler(this.QuickMatchControl_Load);
			((System.ComponentModel.ISupportInitialize)(this.gameBox)).EndInit();
			this.panel1.ResumeLayout(false);
			this.panel1.PerformLayout();
			this.ResumeLayout(false);

        }

        #endregion

				private System.Windows.Forms.ComboBox cbSide;
				private System.Windows.Forms.Label lbPlayers;
        private System.Windows.Forms.PictureBox gameBox;
        private ChatBox picoChat;
				private System.Windows.Forms.CheckBox cbReady;
				private System.Windows.Forms.ImageList imageList1;
				private System.Windows.Forms.Panel panel1;
				private System.Windows.Forms.Button battleExtras;
    }
}