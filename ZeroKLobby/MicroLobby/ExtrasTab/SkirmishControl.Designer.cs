namespace ZeroKLobby.MicroLobby.ExtrasTab
{
    partial class SkirmishControl
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
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.minimapPanel = new System.Windows.Forms.Panel();
            this.metalmapRadioButton = new System.Windows.Forms.RadioButton();
            this.elevationRadioButton = new System.Windows.Forms.RadioButton();
            this.normalRadioButton = new System.Windows.Forms.RadioButton();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.sideCB = new System.Windows.Forms.ComboBox();
            this.gameOptionButton = new ZeroKLobby.BitmapButton();
            this.infoLabel = new System.Windows.Forms.Label();
            this.editTeamButton = new ZeroKLobby.BitmapButton();
            this.addAIButton = new ZeroKLobby.BitmapButton();
            this.spectateCheckBox = new System.Windows.Forms.CheckBox();
            this.startbutton = new ZeroKLobby.BitmapButton();
            this.map_comboBox = new System.Windows.Forms.ComboBox();
            this.lblEngine = new System.Windows.Forms.Label();
            this.game_comboBox = new System.Windows.Forms.ComboBox();
            this.lblGame = new System.Windows.Forms.Label();
            this.engine_comboBox = new System.Windows.Forms.ComboBox();
            this.lbMap = new System.Windows.Forms.Label();
            this.lblSide = new System.Windows.Forms.Label();
            this.skirmPlayerBox = new ZeroKLobby.MicroLobby.PlayerListBox();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
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
            this.splitContainer1.Panel1.Controls.Add(this.minimapPanel);
            this.splitContainer1.Panel1.Controls.Add(this.metalmapRadioButton);
            this.splitContainer1.Panel1.Controls.Add(this.elevationRadioButton);
            this.splitContainer1.Panel1.Controls.Add(this.normalRadioButton);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.splitContainer2);
            this.splitContainer1.Size = new System.Drawing.Size(512, 320);
            this.splitContainer1.SplitterDistance = 275;
            this.splitContainer1.TabIndex = 0;
            // 
            // minimapPanel
            // 
            this.minimapPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.minimapPanel.Location = new System.Drawing.Point(0, 0);
            this.minimapPanel.Name = "minimapPanel";
            this.minimapPanel.Size = new System.Drawing.Size(275, 297);
            this.minimapPanel.TabIndex = 3;
            // 
            // metalmapRadioButton
            // 
            this.metalmapRadioButton.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.metalmapRadioButton.AutoSize = true;
            this.metalmapRadioButton.BackColor = System.Drawing.Color.Transparent;
            this.metalmapRadioButton.Location = new System.Drawing.Point(141, 297);
            this.metalmapRadioButton.Name = "metalmapRadioButton";
            this.metalmapRadioButton.Size = new System.Drawing.Size(97, 17);
            this.metalmapRadioButton.TabIndex = 2;
            this.metalmapRadioButton.Text = "Metalmap View";
            this.metalmapRadioButton.UseVisualStyleBackColor = false;
            this.metalmapRadioButton.CheckedChanged += new System.EventHandler(this.Event_MinimapRadioButton_CheckedChanged);
            // 
            // elevationRadioButton
            // 
            this.elevationRadioButton.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.elevationRadioButton.AutoSize = true;
            this.elevationRadioButton.BackColor = System.Drawing.Color.Transparent;
            this.elevationRadioButton.Location = new System.Drawing.Point(66, 297);
            this.elevationRadioButton.Name = "elevationRadioButton";
            this.elevationRadioButton.Size = new System.Drawing.Size(69, 17);
            this.elevationRadioButton.TabIndex = 1;
            this.elevationRadioButton.Text = "Elevation";
            this.elevationRadioButton.UseVisualStyleBackColor = false;
            this.elevationRadioButton.CheckedChanged += new System.EventHandler(this.Event_MinimapRadioButton_CheckedChanged);
            // 
            // normalRadioButton
            // 
            this.normalRadioButton.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.normalRadioButton.AutoSize = true;
            this.normalRadioButton.BackColor = System.Drawing.Color.Transparent;
            this.normalRadioButton.Checked = true;
            this.normalRadioButton.Location = new System.Drawing.Point(2, 297);
            this.normalRadioButton.Name = "normalRadioButton";
            this.normalRadioButton.Size = new System.Drawing.Size(58, 17);
            this.normalRadioButton.TabIndex = 0;
            this.normalRadioButton.TabStop = true;
            this.normalRadioButton.Text = "Normal";
            this.normalRadioButton.UseVisualStyleBackColor = false;
            this.normalRadioButton.CheckedChanged += new System.EventHandler(this.Event_MinimapRadioButton_CheckedChanged);
            // 
            // splitContainer2
            // 
            this.splitContainer2.BackColor = System.Drawing.Color.DimGray;
            this.splitContainer2.ForeColor = System.Drawing.Color.White;
            this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer2.Location = new System.Drawing.Point(0, 0);
            this.splitContainer2.Name = "splitContainer2";
            this.splitContainer2.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer2.Panel1
            // 
            this.splitContainer2.Panel1.Controls.Add(this.skirmPlayerBox);
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.Controls.Add(this.lblSide);
            this.splitContainer2.Panel2.Controls.Add(this.sideCB);
            this.splitContainer2.Panel2.Controls.Add(this.gameOptionButton);
            this.splitContainer2.Panel2.Controls.Add(this.infoLabel);
            this.splitContainer2.Panel2.Controls.Add(this.editTeamButton);
            this.splitContainer2.Panel2.Controls.Add(this.addAIButton);
            this.splitContainer2.Panel2.Controls.Add(this.spectateCheckBox);
            this.splitContainer2.Panel2.Controls.Add(this.startbutton);
            this.splitContainer2.Panel2.Controls.Add(this.map_comboBox);
            this.splitContainer2.Panel2.Controls.Add(this.lblEngine);
            this.splitContainer2.Panel2.Controls.Add(this.game_comboBox);
            this.splitContainer2.Panel2.Controls.Add(this.lblGame);
            this.splitContainer2.Panel2.Controls.Add(this.engine_comboBox);
            this.splitContainer2.Panel2.Controls.Add(this.lbMap);
            this.splitContainer2.Size = new System.Drawing.Size(233, 320);
            this.splitContainer2.SplitterDistance = 140;
            this.splitContainer2.TabIndex = 0;
            // 
            // sideCB
            // 
            this.sideCB.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.sideCB.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.sideCB.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.sideCB.FormattingEnabled = true;
            this.sideCB.Location = new System.Drawing.Point(40, 106);
            this.sideCB.Name = "sideCB";
            this.sideCB.Size = new System.Drawing.Size(112, 21);
            this.sideCB.TabIndex = 27;
            // 
            // gameOptionButton
            // 
            this.gameOptionButton.BackColor = System.Drawing.Color.Transparent;
            this.gameOptionButton.BackgroundImage = global::ZeroKLobby.Buttons.panel;
            this.gameOptionButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.gameOptionButton.Cursor = System.Windows.Forms.Cursors.Hand;
            this.gameOptionButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.gameOptionButton.ForeColor = System.Drawing.Color.White;
            this.gameOptionButton.Margin = new System.Windows.Forms.Padding(0);
            this.gameOptionButton.Location = new System.Drawing.Point(152, 77);
            this.gameOptionButton.Name = "gameOptionButton";
            this.gameOptionButton.Size = new System.Drawing.Size(78, 23);
            this.gameOptionButton.TabIndex = 26;
            this.gameOptionButton.Text = "Game Option";
            this.gameOptionButton.UseVisualStyleBackColor = true;
            this.gameOptionButton.Click += new System.EventHandler(this.Event_GameOptionButton_Click);
            // 
            // infoLabel
            // 
            this.infoLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.infoLabel.AutoSize = true;
            this.infoLabel.Location = new System.Drawing.Point(84, 152);
            this.infoLabel.Name = "infoLabel";
            this.infoLabel.Size = new System.Drawing.Size(35, 13);
            this.infoLabel.TabIndex = 25;
            this.infoLabel.Text = "label1";
            // 
            // editTeamButton
            // 
            this.editTeamButton.BackColor = System.Drawing.Color.Transparent;
            this.editTeamButton.BackgroundImage = global::ZeroKLobby.Buttons.panel;
            this.editTeamButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.editTeamButton.Cursor = System.Windows.Forms.Cursors.Hand;
            this.editTeamButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.editTeamButton.ForeColor = System.Drawing.Color.White;
            this.editTeamButton.Margin = new System.Windows.Forms.Padding(0);
            this.editTeamButton.Location = new System.Drawing.Point(77, 77);
            this.editTeamButton.Name = "editTeamButton";
            this.editTeamButton.Size = new System.Drawing.Size(75, 23);
            this.editTeamButton.TabIndex = 24;
            this.editTeamButton.Text = "Edit Team";
            this.editTeamButton.UseVisualStyleBackColor = true;
            this.editTeamButton.Click += new System.EventHandler(this.Event_EditTeamButton_Click);
            // 
            // addAIButton
            // 
            this.addAIButton.BackColor = System.Drawing.Color.Transparent;
            this.addAIButton.BackgroundImage = global::ZeroKLobby.Buttons.panel;
            this.addAIButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.addAIButton.Cursor = System.Windows.Forms.Cursors.Hand;
            this.addAIButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.addAIButton.ForeColor = System.Drawing.Color.White;
            this.addAIButton.Margin = new System.Windows.Forms.Padding(0);
            this.addAIButton.Location = new System.Drawing.Point(0, 77);
            this.addAIButton.Name = "addAIButton";
            this.addAIButton.Size = new System.Drawing.Size(75, 23);
            this.addAIButton.TabIndex = 23;
            this.addAIButton.Text = "Add AI";
            this.addAIButton.UseVisualStyleBackColor = true;
            this.addAIButton.Click += new System.EventHandler(this.Event_AddAIButton_Click);
            // 
            // spectateCheckBox
            // 
            this.spectateCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.spectateCheckBox.AutoSize = true;
            this.spectateCheckBox.Location = new System.Drawing.Point(3, 127);
            this.spectateCheckBox.Name = "spectateCheckBox";
            this.spectateCheckBox.Size = new System.Drawing.Size(69, 17);
            this.spectateCheckBox.TabIndex = 22;
            this.spectateCheckBox.Text = "Spectate";
            this.spectateCheckBox.UseVisualStyleBackColor = true;
            this.spectateCheckBox.CheckedChanged += new System.EventHandler(this.Event_SpectateCheckBox_CheckedChanged);
            // 
            // startbutton
            // 
            this.startbutton.BackColor = System.Drawing.Color.Transparent;
            this.startbutton.BackgroundImage = global::ZeroKLobby.Buttons.panel;
            this.startbutton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.startbutton.Cursor = System.Windows.Forms.Cursors.Hand;
            this.startbutton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.startbutton.ForeColor = System.Drawing.Color.White;
            this.startbutton.Margin = new System.Windows.Forms.Padding(0);
            this.startbutton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.startbutton.Location = new System.Drawing.Point(3, 147);
            this.startbutton.Name = "startbutton";
            this.startbutton.Size = new System.Drawing.Size(75, 23);
            this.startbutton.TabIndex = 21;
            this.startbutton.Text = "Start Game";
            this.startbutton.UseVisualStyleBackColor = true;
            this.startbutton.Click += new System.EventHandler(this.Event_Startbutton_Click);
            // 
            // map_comboBox
            // 
            this.map_comboBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.map_comboBox.DropDownHeight = 200;
            this.map_comboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.map_comboBox.DropDownWidth = 200;
            this.map_comboBox.FormattingEnabled = true;
            this.map_comboBox.IntegralHeight = false;
            this.map_comboBox.Location = new System.Drawing.Point(52, 50);
            this.map_comboBox.Name = "map_comboBox";
            this.map_comboBox.Size = new System.Drawing.Size(178, 21);
            this.map_comboBox.TabIndex = 20;
            this.map_comboBox.SelectedIndexChanged += new System.EventHandler(this.Event_ComboBox_SelectedIndexChanged);
            // 
            // lblEngine
            // 
            this.lblEngine.AutoSize = true;
            this.lblEngine.Location = new System.Drawing.Point(3, 6);
            this.lblEngine.Name = "lblEngine";
            this.lblEngine.Size = new System.Drawing.Size(43, 13);
            this.lblEngine.TabIndex = 15;
            this.lblEngine.Text = "Engine:";
            // 
            // game_comboBox
            // 
            this.game_comboBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.game_comboBox.DropDownHeight = 200;
            this.game_comboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.game_comboBox.DropDownWidth = 200;
            this.game_comboBox.FormattingEnabled = true;
            this.game_comboBox.IntegralHeight = false;
            this.game_comboBox.Location = new System.Drawing.Point(52, 27);
            this.game_comboBox.Name = "game_comboBox";
            this.game_comboBox.Size = new System.Drawing.Size(178, 21);
            this.game_comboBox.TabIndex = 19;
            this.game_comboBox.SelectedIndexChanged += new System.EventHandler(this.Event_ComboBox_SelectedIndexChanged);
            // 
            // lblGame
            // 
            this.lblGame.AutoSize = true;
            this.lblGame.Location = new System.Drawing.Point(8, 30);
            this.lblGame.Name = "lblGame";
            this.lblGame.Size = new System.Drawing.Size(38, 13);
            this.lblGame.TabIndex = 16;
            this.lblGame.Text = "Game:";
            // 
            // engine_comboBox
            // 
            this.engine_comboBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.engine_comboBox.DropDownHeight = 200;
            this.engine_comboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.engine_comboBox.DropDownWidth = 200;
            this.engine_comboBox.FormattingEnabled = true;
            this.engine_comboBox.IntegralHeight = false;
            this.engine_comboBox.Location = new System.Drawing.Point(52, 3);
            this.engine_comboBox.Name = "engine_comboBox";
            this.engine_comboBox.Size = new System.Drawing.Size(178, 21);
            this.engine_comboBox.TabIndex = 18;
            this.engine_comboBox.SelectedIndexChanged += new System.EventHandler(this.Event_ComboBox_SelectedIndexChanged);
            // 
            // lbMap
            // 
            this.lbMap.AutoSize = true;
            this.lbMap.Location = new System.Drawing.Point(15, 53);
            this.lbMap.Name = "lbMap";
            this.lbMap.Size = new System.Drawing.Size(31, 13);
            this.lbMap.TabIndex = 17;
            this.lbMap.Text = "Map:";
            // 
            // lblSide
            // 
            this.lblSide.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblSide.AutoSize = true;
            this.lblSide.Location = new System.Drawing.Point(3, 111);
            this.lblSide.Name = "lblSide";
            this.lblSide.Size = new System.Drawing.Size(31, 13);
            this.lblSide.TabIndex = 28;
            this.lblSide.Text = "Side:";
            // 
            // skirmPlayerBox
            // 
            this.skirmPlayerBox.BackColor = System.Drawing.Color.White;
            this.skirmPlayerBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.skirmPlayerBox.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawVariable;
            this.skirmPlayerBox.FormattingEnabled = true;
            this.skirmPlayerBox.HoverItem = null;
            this.skirmPlayerBox.IsBattle = false;
            this.skirmPlayerBox.Location = new System.Drawing.Point(0, 0);
            this.skirmPlayerBox.Name = "skirmPlayerBox";
            this.skirmPlayerBox.Size = new System.Drawing.Size(233, 140);
            this.skirmPlayerBox.TabIndex = 0;
            // 
            // SkirmishControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.Controls.Add(this.splitContainer1);
            this.MinimumSize = new System.Drawing.Size(512, 320);
            this.Name = "SkirmishControl";
            this.Size = new System.Drawing.Size(512, 320);
            this.Resize += new System.EventHandler(this.Event_SkirmishControl_Resize);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel1.PerformLayout();
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.ResumeLayout(false);
            this.splitContainer2.Panel1.ResumeLayout(false);
            this.splitContainer2.Panel2.ResumeLayout(false);
            this.splitContainer2.Panel2.PerformLayout();
            this.splitContainer2.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private System.Windows.Forms.ComboBox map_comboBox;
        private System.Windows.Forms.ComboBox game_comboBox;
        private System.Windows.Forms.ComboBox engine_comboBox;
        private System.Windows.Forms.Label lbMap;
        private System.Windows.Forms.Label lblEngine;
        private System.Windows.Forms.Label lblGame;
        private ZeroKLobby.BitmapButton startbutton;
        private PlayerListBox skirmPlayerBox;
        private System.Windows.Forms.RadioButton normalRadioButton;
        private System.Windows.Forms.RadioButton metalmapRadioButton;
        private System.Windows.Forms.RadioButton elevationRadioButton;
        private System.Windows.Forms.CheckBox spectateCheckBox;
        private System.Windows.Forms.Panel minimapPanel;
        private ZeroKLobby.BitmapButton addAIButton;
        private ZeroKLobby.BitmapButton editTeamButton;
        private System.Windows.Forms.Label infoLabel;
        private ZeroKLobby.BitmapButton gameOptionButton;
        private System.Windows.Forms.ComboBox sideCB;
        private System.Windows.Forms.Label lblSide;
    }
}
