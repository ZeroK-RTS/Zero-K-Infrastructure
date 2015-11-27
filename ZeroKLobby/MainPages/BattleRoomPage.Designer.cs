namespace ZeroKLobby.BattleRoom
{
    partial class BattleRoomPage
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
            if (disposing && (this.components != null))
            {
                this.components.Dispose();
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
            this.battleChatControl1 = new ZeroKLobby.MicroLobby.BattleChatControl();
            this.SuspendLayout();
            // 
            // battleChatControl1
            // 
            this.battleChatControl1.ChannelName = "Battle";
            this.battleChatControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.battleChatControl1.IsTopicVisible = false;
            this.battleChatControl1.Location = new System.Drawing.Point(0, 0);
            this.battleChatControl1.Margin = new System.Windows.Forms.Padding(0);
            this.battleChatControl1.Name = "battleChatControl1";
            this.battleChatControl1.Size = new System.Drawing.Size(585, 471);
            this.battleChatControl1.TabIndex = 0;
            // 
            // BattleRoomPage
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.battleChatControl1);
            this.Name = "BattleRoomPage";
            this.Size = new System.Drawing.Size(585, 471);
            this.ResumeLayout(false);

        }

        #endregion

        private MicroLobby.BattleChatControl battleChatControl1;
    }
}
