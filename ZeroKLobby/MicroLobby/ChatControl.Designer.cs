using System.Drawing;
using System.Windows.Forms;
using ZkData;
using ZeroKLobby;
using ZeroKLobby.Controls;

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
        //public ChatBox ChatBox { get; set; } //note: for some reason I have to declare this at ChatControl.cs instead of let the default else my (VisualC#2010Express) Design-mode throw error.
        //public ChatBox TopicBox { get; set; }
        //public Panel TopicPanel { get; set; }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ChatControl));
            this.playerListMapSplitContainer = new System.Windows.Forms.SplitContainer();
            this.playerBoxSearchBarContainer = new System.Windows.Forms.Panel();
            this.playerBox = new ZeroKLobby.Controls.PlayerListControl();
            this.searchBarContainer = new System.Windows.Forms.TableLayoutPanel();
            this.playerSearchBox = new ZeroKLobby.Controls.ZklTextBox();
            this.sendBox = new ZeroKLobby.MicroLobby.SendBox();
            this.topicPanel = new System.Windows.Forms.Panel();
            this.hideButton = new ZeroKLobby.BitmapButton();
            this.topicBox = new ZeroKLobby.MicroLobby.ChatBox();
            this.ChatBox = new ZeroKLobby.MicroLobby.ChatBox();
            this.splitContainer1 = new ZeroKLobby.ZkSplitContainer();
            ((System.ComponentModel.ISupportInitialize)(this.playerListMapSplitContainer)).BeginInit();
            this.playerListMapSplitContainer.Panel1.SuspendLayout();
            this.playerListMapSplitContainer.SuspendLayout();
            this.playerBoxSearchBarContainer.SuspendLayout();
            this.searchBarContainer.SuspendLayout();
            this.topicPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.SuspendLayout();
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
            this.playerBox.ForeColor = System.Drawing.Color.White;
            this.playerBox.HoverItem = null;
            this.playerBox.IsBattle = false;
            this.playerBox.IsSorted = false;
            this.playerBox.Location = new System.Drawing.Point(0, 24);
            this.playerBox.Name = "playerBox";
            this.playerBox.SelectedItem = null;
            this.playerBox.Size = new System.Drawing.Size(326, 541);
            this.playerBox.TabIndex = 1;
            // 
            // searchBarContainer
            // 
            this.searchBarContainer.ColumnCount = 2;
            this.searchBarContainer.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize, 20F));
            this.searchBarContainer.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.searchBarContainer.Controls.Add(this.playerSearchBox, 1, 0);
            this.searchBarContainer.Dock = System.Windows.Forms.DockStyle.Top;
            this.searchBarContainer.Location = new System.Drawing.Point(0, 0);
            this.searchBarContainer.Name = "searchBarContainer";
            this.searchBarContainer.RowCount = 1;
            this.searchBarContainer.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 25F));
            this.searchBarContainer.Size = new System.Drawing.Size(326, 30);
            this.searchBarContainer.AutoSize = false;
            this.searchBarContainer.TabIndex = 0;
            // 
            // playerSearchBox
            // 
            this.playerSearchBox.Anchor = AnchorStyles.Left | AnchorStyles.Top;
            this.playerSearchBox.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(30)))), ((int)(((byte)(40)))));
            this.playerSearchBox.Location = new System.Drawing.Point(23, 20);
            this.playerSearchBox.Name = "playerSearchBox";
            this.playerSearchBox.Size = new System.Drawing.Size(300, 20);
            this.playerSearchBox.Margin = new Padding(0);
            this.playerSearchBox.TabIndex = 0;
            this.playerSearchBox.Font = Config.GeneralFontSmall;
            this.playerSearchBox.TextChanged += new System.EventHandler(this.playerSearchBox_TextChanged);
            // 
            // sendBox
            // 
            this.sendBox.AcceptsTab = true;
            this.sendBox.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(30)))), ((int)(((byte)(40)))));
            this.sendBox.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.sendBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 15F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Pixel);
            this.sendBox.ForeColor = System.Drawing.Color.White;
            this.sendBox.Location = new System.Drawing.Point(0, 765);
            this.sendBox.Multiline = true;
            this.sendBox.Name = "sendBox";
            this.sendBox.Size = new System.Drawing.Size(800, 28);
            this.sendBox.TabIndex = 0;
            this.sendBox.WordWrap = false;
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
            this.hideButton.ButtonStyle = ZeroKLobby.FrameBorderRenderer.StyleType.DarkHive;
            this.hideButton.Cursor = System.Windows.Forms.Cursors.Hand;
            this.hideButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.hideButton.ForeColor = System.Drawing.Color.White;
            this.hideButton.Location = new System.Drawing.Point(711, -29);
            this.hideButton.Name = "hideButton";
            this.hideButton.Size = new System.Drawing.Size(75, 23);
            this.hideButton.SoundType = ZeroKLobby.Controls.SoundPalette.SoundType.Click;
            this.hideButton.TabIndex = 3;
            this.hideButton.Text = "Hide";
            this.hideButton.UseVisualStyleBackColor = true;
            this.hideButton.Click += new System.EventHandler(this.hideButton_Click);
            // 
            // topicBox
            // 
            this.topicBox.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(30)))), ((int)(((byte)(40)))));
            this.topicBox.ChatBackgroundColor = 0;
            this.topicBox.DefaultTooltip = null;
            this.topicBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.topicBox.Font = new System.Drawing.Font("Verdana", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.topicBox.HideScroll = false;
            this.topicBox.IRCForeColor = 0;
            this.topicBox.LineHighlight = null;
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
            // 
            // ChatBox
            // 
            this.ChatBox.BackColor = System.Drawing.Color.DimGray;
            this.ChatBox.ChatBackgroundColor = 0;
            this.ChatBox.DefaultTooltip = null;
            this.ChatBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ChatBox.Font = new System.Drawing.Font("Verdana", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ChatBox.ForeColor = System.Drawing.Color.White;
            this.ChatBox.HideScroll = false;
            this.ChatBox.IRCForeColor = 0;
            this.ChatBox.LineHighlight = null;
            this.ChatBox.Location = new System.Drawing.Point(0, 0);
            this.ChatBox.Name = "ChatBox";
            this.ChatBox.NoColorMode = false;
            this.ChatBox.ShowHistory = true;
            this.ChatBox.ShowJoinLeave = false;
            this.ChatBox.ShowUnreadLine = true;
            this.ChatBox.SingleLine = false;
            this.ChatBox.Size = new System.Drawing.Size(800, 765);
            this.ChatBox.TabIndex = 1;
            this.ChatBox.TextFilter = null;
            // 
            // splitContainer1
            // 
            this.splitContainer1.BackColor = System.Drawing.Color.Transparent;
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Margin = new System.Windows.Forms.Padding(0);
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
            // ChatControl
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.Controls.Add(this.splitContainer1);
            this.Margin = new System.Windows.Forms.Padding(0);
            this.Name = "ChatControl";
            this.Size = new System.Drawing.Size(1130, 793);
            this.playerListMapSplitContainer.Panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.playerListMapSplitContainer)).EndInit();
            this.playerListMapSplitContainer.ResumeLayout(false);
            this.playerBoxSearchBarContainer.ResumeLayout(false);
            this.searchBarContainer.ResumeLayout(false);
            this.topicPanel.ResumeLayout(false);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel1.PerformLayout();
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.ResumeLayout(false);

        }



        #endregion

        protected SplitContainer playerListMapSplitContainer;
        protected Panel playerBoxSearchBarContainer;
        public PlayerListControl playerBox;
        private TableLayoutPanel searchBarContainer;
        protected ZklTextBox playerSearchBox;
        private Panel topicPanel;
        private BitmapButton hideButton;
        private ChatBox topicBox;
        public ChatBox ChatBox;
        private ZkSplitContainer splitContainer1;
        public SendBox sendBox;


    }
}
