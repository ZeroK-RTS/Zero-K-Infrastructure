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
            this.lbPlayers = new System.Windows.Forms.Label();
            this.imageList1 = new System.Windows.Forms.ImageList(this.components);
            this.panel1 = new System.Windows.Forms.Panel();
            this.btnLeave = new ZeroKLobby.BitmapButton();
            this.picoChat = new ZeroKLobby.MicroLobby.ChatBox();
            this.gameBox = new System.Windows.Forms.PictureBox();
            this.btnStart = new ZeroKLobby.BitmapButton();
            this.radioPlaySpecContainer = new System.Windows.Forms.TableLayoutPanel();
            this.radioPlay = new System.Windows.Forms.RadioButton();
            this.radioSpec = new System.Windows.Forms.RadioButton();
            this.lbQueue = new System.Windows.Forms.Label();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gameBox)).BeginInit();
            this.radioPlaySpecContainer.SuspendLayout();
            this.SuspendLayout();
            // 
            // lbPlayers
            // 
            this.lbPlayers.AutoSize = true;
            this.lbPlayers.Location = new System.Drawing.Point(68, 13);
            this.lbPlayers.Name = "lbPlayers";
            this.lbPlayers.Size = new System.Drawing.Size(0, 18);
            this.lbPlayers.TabIndex = 3;
            // 
            // imageList1
            // 
            this.imageList1.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList1.ImageStream")));
            this.imageList1.TransparentColor = System.Drawing.Color.Transparent;
            this.imageList1.Images.SetKeyName(0, "joined.ico");
            this.imageList1.Images.SetKeyName(1, "ok.ico");
            this.imageList1.Images.SetKeyName(2, "run.ico");
            this.imageList1.Images.SetKeyName(3, "spec.png");
            this.imageList1.Images.SetKeyName(4, "quickmatch_off.png");
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.btnLeave);
            this.panel1.Controls.Add(this.picoChat);
            this.panel1.Controls.Add(this.gameBox);
            this.panel1.Controls.Add(this.btnStart);
            this.panel1.Controls.Add(this.radioPlaySpecContainer);
            this.panel1.Controls.Add(this.lbPlayers);
            this.panel1.Controls.Add(this.lbQueue);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Padding = new System.Windows.Forms.Padding(0, 8, 0, 8);
            this.panel1.Size = new System.Drawing.Size(1222, 96);
            this.panel1.TabIndex = 15;
            // 
            // buttonLeave
            // 
            this.btnLeave.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnLeave.BackColor = System.Drawing.Color.Transparent;
            this.btnLeave.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.btnLeave.ButtonStyle = ZeroKLobby.FrameBorderRenderer.StyleType.DarkHive;
            this.btnLeave.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnLeave.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnLeave.ForeColor = System.Drawing.Color.White;
            this.btnLeave.Image = global::ZeroKLobby.Buttons.exit;
            this.btnLeave.Location = new System.Drawing.Point(1134, 12);
            this.btnLeave.Name = "btnLeave";
            this.btnLeave.Size = new System.Drawing.Size(68, 68);
            this.btnLeave.SoundType = ZeroKLobby.Controls.SoundPalette.SoundType.Click;
            this.btnLeave.TabIndex = 12;
            this.btnLeave.UseVisualStyleBackColor = false;
            // 
            // picoChat
            // 
            this.picoChat.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.picoChat.BackColor = System.Drawing.Color.Transparent;
            this.picoChat.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.picoChat.ChatBackgroundColor = 0;
            this.picoChat.DefaultTooltip = null;
            this.picoChat.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.picoChat.HideScroll = true;
            this.picoChat.IRCForeColor = 0;
            this.picoChat.LineHighlight = null;
            this.picoChat.Location = new System.Drawing.Point(537, 8);
            this.picoChat.Margin = new System.Windows.Forms.Padding(0, 0, 3, 0);
            this.picoChat.MinimumSize = new System.Drawing.Size(12, 12);
            this.picoChat.Name = "picoChat";
            this.picoChat.NoColorMode = false;
            this.picoChat.ShowHistory = true;
            this.picoChat.ShowJoinLeave = false;
            this.picoChat.ShowUnreadLine = true;
            this.picoChat.SingleLine = false;
            this.picoChat.Size = new System.Drawing.Size(591, 80);
            this.picoChat.TabIndex = 12;
            this.picoChat.TextFilter = null;
            // 
            // gameBox
            // 
            this.gameBox.Location = new System.Drawing.Point(217, 8);
            this.gameBox.Margin = new System.Windows.Forms.Padding(0);
            this.gameBox.Name = "gameBox";
            this.gameBox.Size = new System.Drawing.Size(311, 76);
            this.gameBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
            this.gameBox.TabIndex = 11;
            this.gameBox.TabStop = false;
            // 
            // btnDetail
            // 
            this.btnStart.BackColor = System.Drawing.Color.Transparent;
            this.btnStart.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.btnStart.ButtonStyle = ZeroKLobby.FrameBorderRenderer.StyleType.DarkHive;
            this.btnStart.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnStart.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnStart.ForeColor = System.Drawing.Color.White;
            this.btnStart.Image = global::ZeroKLobby.ZklResources.battle;
            this.btnStart.Location = new System.Drawing.Point(15, 12);
            this.btnStart.Name = "btnStart";
            this.btnStart.Size = new System.Drawing.Size(68, 68);
            this.btnStart.SoundType = ZeroKLobby.Controls.SoundPalette.SoundType.Click;
            this.btnStart.TabIndex = 13;
            this.btnStart.UseVisualStyleBackColor = false;
            // 
            // radioPlaySpecContainer
            // 
            this.radioPlaySpecContainer.AutoSize = true;
            this.radioPlaySpecContainer.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.radioPlaySpecContainer.ColumnCount = 1;
            this.radioPlaySpecContainer.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.radioPlaySpecContainer.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.radioPlaySpecContainer.Controls.Add(this.radioPlay, 0, 0);
            this.radioPlaySpecContainer.Controls.Add(this.radioSpec, 0, 1);
            this.radioPlaySpecContainer.Location = new System.Drawing.Point(98, 15);
            this.radioPlaySpecContainer.Name = "radioPlaySpecContainer";
            this.radioPlaySpecContainer.RowCount = 2;
            this.radioPlaySpecContainer.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.radioPlaySpecContainer.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.radioPlaySpecContainer.Size = new System.Drawing.Size(116, 56);
            this.radioPlaySpecContainer.TabIndex = 18;
            // 
            // radioPlay
            // 
            this.radioPlay.Checked = true;
            this.radioPlay.Image = global::ZeroKLobby.ZklResources.game;
            this.radioPlay.ImageAlign = System.Drawing.ContentAlignment.TopRight;
            this.radioPlay.Location = new System.Drawing.Point(2, 2);
            this.radioPlay.Margin = new System.Windows.Forms.Padding(2);
            this.radioPlay.Name = "radioPlay";
            this.radioPlay.Size = new System.Drawing.Size(112, 24);
            this.radioPlay.TabIndex = 16;
            this.radioPlay.TabStop = true;
            this.radioPlay.Text = "Play";
            this.radioPlay.TextImageRelation = System.Windows.Forms.TextImageRelation.TextBeforeImage;
            this.radioPlay.UseVisualStyleBackColor = true;
            this.radioPlay.CheckedChanged += new System.EventHandler(this.radioPlay_CheckedChanged);
            // 
            // radioSpec
            // 
            this.radioSpec.Image = global::ZeroKLobby.ZklResources.away1;
            this.radioSpec.ImageAlign = System.Drawing.ContentAlignment.TopRight;
            this.radioSpec.Location = new System.Drawing.Point(2, 30);
            this.radioSpec.Margin = new System.Windows.Forms.Padding(2);
            this.radioSpec.Name = "radioSpec";
            this.radioSpec.Size = new System.Drawing.Size(112, 24);
            this.radioSpec.TabIndex = 16;
            this.radioSpec.Text = "Spectate";
            this.radioSpec.TextImageRelation = System.Windows.Forms.TextImageRelation.TextBeforeImage;
            this.radioSpec.UseVisualStyleBackColor = true;
            this.radioSpec.CheckedChanged += new System.EventHandler(this.radioSpec_CheckedChanged);
            // 
            // lbQueue
            // 
            this.lbQueue.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.lbQueue.Location = new System.Drawing.Point(3, 3);
            this.lbQueue.Name = "lbQueue";
            this.lbQueue.Size = new System.Drawing.Size(141, 73);
            this.lbQueue.TabIndex = 17;
            this.lbQueue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lbQueue.Visible = false;
            // 
            // BattleBar
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 18F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Transparent;
            this.Controls.Add(this.panel1);
            this.MinimumSize = new System.Drawing.Size(492, 96);
            this.Name = "BattleBar";
            this.Size = new System.Drawing.Size(1222, 96);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gameBox)).EndInit();
            this.radioPlaySpecContainer.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label lbPlayers;
        private System.Windows.Forms.PictureBox gameBox;
        private ChatBox picoChat;
				private System.Windows.Forms.ImageList imageList1;
                private System.Windows.Forms.Panel panel1;
                private System.Windows.Forms.RadioButton radioPlay;
                private System.Windows.Forms.RadioButton radioSpec;
                private System.Windows.Forms.Label lbQueue;
        private System.Windows.Forms.TableLayoutPanel radioPlaySpecContainer;
        private BitmapButton btnLeave;
        private BitmapButton btnStart;
    }
}