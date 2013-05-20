using System.Windows.Forms;
using PlasmaShared;
using ZeroKLobby;

namespace ZeroKLobby.MicroLobby
{
    partial class ChatControl
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
            if (Program.TasClient != null) Program.TasClient.UnsubscribeEvents(this);
            
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
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.ChatBox = new ZeroKLobby.MicroLobby.ChatBox();
            this.TopicPanel = new System.Windows.Forms.Panel();
            this.hideButton = new ZeroKLobby.BitmapButton();
            this.TopicBox = new ZeroKLobby.MicroLobby.ChatBox();
            this.sendBox = new ZeroKLobby.MicroLobby.SendBox();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.playerBoxSearchBarContainer = new System.Windows.Forms.Panel();
            this.playerBox = new ZeroKLobby.MicroLobby.PlayerListBox();
            this.searchBarContainer = new System.Windows.Forms.Panel();
            this.playerSearchBox = new System.Windows.Forms.TextBox();
            this.mapPanel = new System.Windows.Forms.Panel();
            //((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.TopicPanel.SuspendLayout();
            //((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).BeginInit();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            this.playerBoxSearchBarContainer.SuspendLayout();
            this.searchBarContainer.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.ChatBox);
            this.splitContainer1.Panel1.Controls.Add(this.TopicPanel);
            this.splitContainer1.Panel1.Controls.Add(this.sendBox);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.splitContainer2);
            this.splitContainer1.Size = new System.Drawing.Size(1130, 793);
            this.splitContainer1.SplitterDistance = 850;
            this.splitContainer1.TabIndex = 0;
            this.splitContainer1.SplitterMoved += new System.Windows.Forms.SplitterEventHandler(this.splitContainer1_SplitterMoved);
            // 
            // ChatBox
            // 
            this.ChatBox.BackColor = System.Drawing.SystemColors.Window;
            this.ChatBox.ChatBackgroundColor = 0;
            this.ChatBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ChatBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
            this.ChatBox.HideScroll = false;
            this.ChatBox.IRCForeColor = 0;
            this.ChatBox.Location = new System.Drawing.Point(0, 0);
            this.ChatBox.Name = "ChatBox";
            this.ChatBox.NoColorMode = false;
            this.ChatBox.ShowHistory = true;
            this.ChatBox.ShowJoinLeave = false;
            this.ChatBox.ShowUnreadLine = true;
            this.ChatBox.SingleLine = false;
            this.ChatBox.Size = new System.Drawing.Size(850, 773);
            this.ChatBox.TabIndex = 1;
            this.ChatBox.TextFilter = null;
            this.ChatBox.TotalDisplayLines = 0;
            this.ChatBox.UseTopicBackground = false;
            // 
            // TopicPanel
            // 
            this.TopicPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.TopicPanel.Controls.Add(this.hideButton);
            this.TopicPanel.Controls.Add(this.TopicBox);
            this.TopicPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.TopicPanel.Location = new System.Drawing.Point(0, 0);
            this.TopicPanel.Margin = new System.Windows.Forms.Padding(0);
            this.TopicPanel.Name = "TopicPanel";
            this.TopicPanel.Size = new System.Drawing.Size(850, 0);
            this.TopicPanel.TabIndex = 3;
            // 
            // hideButton
            // 
            this.hideButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.hideButton.Location = new System.Drawing.Point(761, -29);
            this.hideButton.Name = "hideButton";
            this.hideButton.Size = new System.Drawing.Size(75, 23);
            this.hideButton.TabIndex = 3;
            this.hideButton.Text = "Hide";
            this.hideButton.UseVisualStyleBackColor = true;
            this.hideButton.Click += new System.EventHandler(this.hideButton_Click);
            // 
            // TopicBox
            // 
            this.TopicBox.ChatBackgroundColor = 0;
            this.TopicBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.TopicBox.Font = new System.Drawing.Font("Verdana", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.TopicBox.HideScroll = false;
            this.TopicBox.IRCForeColor = 0;
            this.TopicBox.Location = new System.Drawing.Point(0, 0);
            this.TopicBox.Name = "TopicBox";
            this.TopicBox.NoColorMode = false;
            this.TopicBox.ShowHistory = true;
            this.TopicBox.ShowJoinLeave = false;
            this.TopicBox.ShowUnreadLine = true;
            this.TopicBox.SingleLine = false;
            this.TopicBox.Size = new System.Drawing.Size(850, 0);
            this.TopicBox.TabIndex = 2;
            this.TopicBox.TextFilter = null;
            this.TopicBox.TotalDisplayLines = 0;
            this.TopicBox.UseTopicBackground = false;
            this.TopicBox.Visible = false;
            // 
            // sendBox
            // 
            this.sendBox.AcceptsTab = true;
            this.sendBox.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.sendBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
            this.sendBox.Location = new System.Drawing.Point(0, 773);
            this.sendBox.Multiline = true;
            this.sendBox.Name = "sendBox";
            this.sendBox.Size = new System.Drawing.Size(850, 20);
            this.sendBox.TabIndex = 0;
            // 
            // splitContainer2
            // 
            this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer2.Location = new System.Drawing.Point(0, 0);
            this.splitContainer2.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.splitContainer2.Name = "splitContainer2";
            this.splitContainer2.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer2.Panel1
            // 
            this.splitContainer2.Panel1.Controls.Add(this.playerBoxSearchBarContainer);
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.AutoScroll = true;
            this.splitContainer2.Panel2.Controls.Add(this.mapPanel);
            this.splitContainer2.Panel2Collapsed = true;
            this.splitContainer2.Size = new System.Drawing.Size(276, 793);
            this.splitContainer2.SplitterDistance = 602;
            this.splitContainer2.SplitterWidth = 3;
            this.splitContainer2.TabIndex = 0;
            this.splitContainer2.SplitterMoved += new System.Windows.Forms.SplitterEventHandler(this.splitContainer2_SplitterMoved);
            // 
            // playerBoxSearchBarContainer
            // 
            this.playerBoxSearchBarContainer.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.playerBoxSearchBarContainer.Controls.Add(this.playerBox);
            this.playerBoxSearchBarContainer.Controls.Add(this.searchBarContainer);
            this.playerBoxSearchBarContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.playerBoxSearchBarContainer.Location = new System.Drawing.Point(0, 0);
            this.playerBoxSearchBarContainer.Name = "playerBoxSearchBarContainer";
            this.playerBoxSearchBarContainer.Size = new System.Drawing.Size(276, 793);
            this.playerBoxSearchBarContainer.TabIndex = 2;
            // 
            // playerBox
            // 
            this.playerBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.playerBox.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.playerBox.FormattingEnabled = true;
            this.playerBox.HoverItem = null;
            this.playerBox.IsBattle = false;
            this.playerBox.Location = new System.Drawing.Point(0, 20);
            this.playerBox.Name = "playerBox";
            this.playerBox.Size = new System.Drawing.Size(276, 773);
            this.playerBox.TabIndex = 1;
            this.playerBox.MouseClick += new System.Windows.Forms.MouseEventHandler(this.playerBox_MouseClick);
            this.playerBox.DoubleClick += new System.EventHandler(this.playerBox_DoubleClick);
            // 
            // searchBarContainer
            // 
            this.searchBarContainer.Controls.Add(this.playerSearchBox);
            this.searchBarContainer.Dock = System.Windows.Forms.DockStyle.Top;
            this.searchBarContainer.Location = new System.Drawing.Point(0, 0);
            this.searchBarContainer.Name = "searchBarContainer";
            this.searchBarContainer.Size = new System.Drawing.Size(276, 20);
            this.searchBarContainer.TabIndex = 0;
            // 
            // playerSearchBox
            // 
            this.playerSearchBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.playerSearchBox.Location = new System.Drawing.Point(20, 0);
            this.playerSearchBox.Name = "playerSearchBox";
            this.playerSearchBox.Size = new System.Drawing.Size(253, 20);
            this.playerSearchBox.TabIndex = 0;
            this.playerSearchBox.TextChanged += new System.EventHandler(this.playerSearchBox_TextChanged);
            // 
            // mapPanel
            // 
            this.mapPanel.AutoSize = true;
            this.mapPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.mapPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mapPanel.Location = new System.Drawing.Point(0, 0);
            this.mapPanel.Name = "mapPanel";
            this.mapPanel.Size = new System.Drawing.Size(112, 37);
            this.mapPanel.TabIndex = 2;
            this.mapPanel.Visible = false;
            // 
            // ChatControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.splitContainer1);
            this.Name = "ChatControl";
            this.Size = new System.Drawing.Size(1130, 793);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel1.PerformLayout();
            this.splitContainer1.Panel2.ResumeLayout(false);
            //((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.TopicPanel.ResumeLayout(false);
            this.splitContainer2.Panel1.ResumeLayout(false);
            this.splitContainer2.Panel2.ResumeLayout(false);
            this.splitContainer2.Panel2.PerformLayout();
            //((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).EndInit();
            this.splitContainer2.ResumeLayout(false);
            this.playerBoxSearchBarContainer.ResumeLayout(false);
            this.searchBarContainer.ResumeLayout(false);
            this.searchBarContainer.PerformLayout();
            this.ResumeLayout(false);

        }



        #endregion

        private System.Windows.Forms.SplitContainer splitContainer1;
        private SendBox sendBox;
        protected PlayerListBox playerBox;
        private System.Windows.Forms.Panel searchBarContainer;
        private System.Windows.Forms.TextBox playerSearchBox;
        //public ChatBox ChatBox { get; set; } //note: for some reason I have to declare this at ChatControl.cs instead of let the default else my (VisualC#2010Express) Design-mode throw error.
        //public ChatBox TopicBox { get; set; }
        //public Panel TopicPanel { get; set; }
        private System.Windows.Forms.Button hideButton;
        private System.Windows.Forms.Panel playerBoxSearchBarContainer;
        protected System.Windows.Forms.Panel mapPanel;
        protected SplitContainer splitContainer2;

    }
}
