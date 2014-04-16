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

        private ZeroKLobby.ZkSplitContainer splitContainer1;
        protected System.Windows.Forms.SplitContainer playerListMapSplitContainer;
        private SendBox sendBox;
        public PlayerListBox playerBox;
        private System.Windows.Forms.Panel searchBarContainer;
        private System.Windows.Forms.TextBox playerSearchBox;
        //public ChatBox ChatBox { get; set; } //note: for some reason I have to declare this at ChatControl.cs instead of let the default else my (VisualC#2010Express) Design-mode throw error.
        //public ChatBox TopicBox { get; set; }
        //public Panel TopicPanel { get; set; }
        protected System.Windows.Forms.Panel playerBoxSearchBarContainer;
        protected System.Windows.Forms.Panel mapPanel;
        private BitmapButton hideButton;

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ChatControl));
            this.splitContainer1 = new ZeroKLobby.ZkSplitContainer();
            this.ChatBox = new ZeroKLobby.MicroLobby.ChatBox();
            this.topicPanel = new System.Windows.Forms.Panel();
            this.hideButton = new ZeroKLobby.BitmapButton();
            this.topicBox = new ZeroKLobby.MicroLobby.ChatBox();
            this.sendBox = new ZeroKLobby.MicroLobby.SendBox();
            this.playerListMapSplitContainer = new System.Windows.Forms.SplitContainer();
            this.playerBoxSearchBarContainer = new System.Windows.Forms.Panel();
            this.playerBox = new ZeroKLobby.MicroLobby.PlayerListBox();
            this.searchBarContainer = new System.Windows.Forms.Panel();
            this.playerSearchBox = new System.Windows.Forms.TextBox();
            this.mapPanel = new System.Windows.Forms.Panel();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.topicPanel.SuspendLayout();
            this.playerListMapSplitContainer.Panel1.SuspendLayout();
            this.playerListMapSplitContainer.Panel2.SuspendLayout();
            this.playerListMapSplitContainer.SuspendLayout();
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
            this.splitContainer1.Panel1.Controls.Add(this.topicPanel);
            this.splitContainer1.Panel1.Controls.Add(this.sendBox);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.playerListMapSplitContainer);
            this.splitContainer1.Size = new System.Drawing.Size(1130, 793);
            this.splitContainer1.SplitterDistance = 800;
            this.splitContainer1.TabIndex = 0;
            this.splitContainer1.SplitterMoved += new System.Windows.Forms.SplitterEventHandler(this.splitContainer1_SplitterMoved);
            // 
            // ChatBox
            // 
            this.ChatBox.BackColor = System.Drawing.Color.DimGray;
            this.ChatBox.ChatBackgroundColor = 0;
            this.ChatBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ChatBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
            this.ChatBox.ForeColor = System.Drawing.Color.White;
            this.ChatBox.HideScroll = false;
            this.ChatBox.IRCForeColor = 0;
            this.ChatBox.Location = new System.Drawing.Point(0, 0);
            this.ChatBox.Name = "ChatBox";
            this.ChatBox.NoColorMode = false;
            this.ChatBox.ShowHistory = true;
            this.ChatBox.ShowJoinLeave = false;
            this.ChatBox.ShowUnreadLine = true;
            this.ChatBox.SingleLine = false;
            this.ChatBox.Size = new System.Drawing.Size(800, 773);
            this.ChatBox.TabIndex = 1;
            this.ChatBox.TextFilter = null;
            this.ChatBox.TotalDisplayLines = 0;
            // 
            // topicPanel
            // 
            this.topicPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.topicPanel.Controls.Add(this.hideButton);
            this.topicPanel.Controls.Add(this.topicBox);
            this.topicPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.topicPanel.Location = new System.Drawing.Point(0, 0);
            this.topicPanel.Margin = new System.Windows.Forms.Padding(0);
            this.topicPanel.Name = "topicPanel";
            this.topicPanel.Size = new System.Drawing.Size(800, 0);
            this.topicPanel.TabIndex = 3;
            // 
            // hideButton
            // 
            this.hideButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.hideButton.BackColor = System.Drawing.Color.Transparent;
            this.hideButton.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("hideButton.BackgroundImage")));
            this.hideButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.hideButton.Cursor = System.Windows.Forms.Cursors.Hand;
            this.hideButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.hideButton.ForeColor = System.Drawing.Color.White;
            this.hideButton.Location = new System.Drawing.Point(711, -29);
            this.hideButton.Name = "hideButton";
            this.hideButton.Size = new System.Drawing.Size(75, 23);
            this.hideButton.TabIndex = 3;
            this.hideButton.Text = "Hide";
            this.hideButton.UseVisualStyleBackColor = true;
            this.hideButton.Click += new System.EventHandler(this.hideButton_Click);
            // 
            // topicBox
            // 
            this.topicBox.ChatBackgroundColor = 0;
            this.topicBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.topicBox.Font = new System.Drawing.Font("Verdana", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.topicBox.HideScroll = false;
            this.topicBox.IRCForeColor = 0;
            this.topicBox.Location = new System.Drawing.Point(0, 0);
            this.topicBox.Name = "topicBox";
            this.topicBox.NoColorMode = false;
            this.topicBox.ShowHistory = true;
            this.topicBox.ShowJoinLeave = false;
            this.topicBox.ShowUnreadLine = true;
            this.topicBox.SingleLine = false;
            this.topicBox.Size = new System.Drawing.Size(800, 0);
            this.topicBox.TabIndex = 2;
            this.topicBox.TextFilter = null;
            this.topicBox.TotalDisplayLines = 0;
            // 
            // sendBox
            // 
            this.sendBox.AcceptsTab = true;
            this.sendBox.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.sendBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
            this.sendBox.Location = new System.Drawing.Point(0, 773);
            this.sendBox.Multiline = true;
            this.sendBox.Name = "sendBox";
            this.sendBox.Size = new System.Drawing.Size(800, 20);
            this.sendBox.TabIndex = 0;
            this.sendBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.sendBox_KeyDown);
            // 
            // playerListMapSplitContainer
            // 
            this.playerListMapSplitContainer.BackColor = System.Drawing.Color.DimGray;
            this.playerListMapSplitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.playerListMapSplitContainer.Location = new System.Drawing.Point(0, 0);
            this.playerListMapSplitContainer.Margin = new System.Windows.Forms.Padding(2);
            this.playerListMapSplitContainer.Name = "playerListMapSplitContainer";
            this.playerListMapSplitContainer.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // playerListMapSplitContainer.Panel1
            // 
            this.playerListMapSplitContainer.Panel1.Controls.Add(this.playerBoxSearchBarContainer);
            // 
            // playerListMapSplitContainer.Panel2
            // 
            this.playerListMapSplitContainer.Panel2.AutoScroll = true;
            this.playerListMapSplitContainer.Panel2.Controls.Add(this.mapPanel);
            this.playerListMapSplitContainer.Size = new System.Drawing.Size(326, 793);
            this.playerListMapSplitContainer.SplitterDistance = 565;
            this.playerListMapSplitContainer.SplitterWidth = 3;
            this.playerListMapSplitContainer.TabIndex = 0;
            this.playerListMapSplitContainer.SplitterMoved += new System.Windows.Forms.SplitterEventHandler(this.playerListMapSplitContainer_SplitterMoved);
            // 
            // playerBoxSearchBarContainer
            // 
            this.playerBoxSearchBarContainer.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.playerBoxSearchBarContainer.Controls.Add(this.playerBox);
            this.playerBoxSearchBarContainer.Controls.Add(this.searchBarContainer);
            this.playerBoxSearchBarContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.playerBoxSearchBarContainer.Location = new System.Drawing.Point(0, 0);
            this.playerBoxSearchBarContainer.Name = "playerBoxSearchBarContainer";
            this.playerBoxSearchBarContainer.Size = new System.Drawing.Size(326, 565);
            this.playerBoxSearchBarContainer.TabIndex = 2;
            // 
            // playerBox
            // 
            this.playerBox.BackColor = System.Drawing.Color.DimGray;
            this.playerBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.playerBox.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.playerBox.ForeColor = System.Drawing.Color.White;
            this.playerBox.FormattingEnabled = true;
            this.playerBox.HoverItem = null;
            this.playerBox.IsBattle = false;
            this.playerBox.Location = new System.Drawing.Point(0, 20);
            this.playerBox.Name = "playerBox";
            this.playerBox.Size = new System.Drawing.Size(326, 545);
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
            this.searchBarContainer.Size = new System.Drawing.Size(326, 20);
            this.searchBarContainer.TabIndex = 0;
            // 
            // playerSearchBox
            // 
            this.playerSearchBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.playerSearchBox.Location = new System.Drawing.Point(20, 0);
            this.playerSearchBox.Name = "playerSearchBox";
            this.playerSearchBox.Size = new System.Drawing.Size(303, 20);
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
            this.mapPanel.Size = new System.Drawing.Size(222, 225);
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
            this.splitContainer1.ResumeLayout(false);
            this.topicPanel.ResumeLayout(false);
            this.playerListMapSplitContainer.Panel1.ResumeLayout(false);
            this.playerListMapSplitContainer.Panel2.ResumeLayout(false);
            this.playerListMapSplitContainer.ResumeLayout(false);
            this.playerBoxSearchBarContainer.ResumeLayout(false);
            this.searchBarContainer.ResumeLayout(false);
            this.searchBarContainer.PerformLayout();
            this.ResumeLayout(false);

        }



        #endregion

    }
}
