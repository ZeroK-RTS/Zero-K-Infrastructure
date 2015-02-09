using System.Drawing;
using System.Windows.Forms;

namespace ZeroKLobby
{
    partial class WelcomeForm
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(WelcomeForm));
            this.label1 = new System.Windows.Forms.Label();
            this.mainFrame = new System.Windows.Forms.Panel();
            this.btnSnd = new ZeroKLobby.BitmapButton();
            this.btnWindowed = new ZeroKLobby.BitmapButton();
            this.exitButton = new ZeroKLobby.BitmapButton();
            this.multiplayerButton = new ZeroKLobby.BitmapButton();
            this.avatarButton = new ZeroKLobby.BitmapButton();
            this.singleplayerButton = new ZeroKLobby.BitmapButton();
            this.mainFrame.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.BackColor = System.Drawing.Color.Transparent;
            this.label1.Font = new System.Drawing.Font("Verdana", 24F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.label1.ForeColor = System.Drawing.Color.White;
            this.label1.Location = new System.Drawing.Point(106, 45);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(227, 38);
            this.label1.TabIndex = 1;
            this.label1.Text = "not logged in";
            // 
            // mainFrame
            // 
            this.mainFrame.BackColor = System.Drawing.Color.Transparent;
            this.mainFrame.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.mainFrame.Controls.Add(this.btnSnd);
            this.mainFrame.Controls.Add(this.btnWindowed);
            this.mainFrame.Controls.Add(this.exitButton);
            this.mainFrame.Controls.Add(this.multiplayerButton);
            this.mainFrame.Controls.Add(this.avatarButton);
            this.mainFrame.Controls.Add(this.label1);
            this.mainFrame.Controls.Add(this.singleplayerButton);
            this.mainFrame.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainFrame.Location = new System.Drawing.Point(0, 0);
            this.mainFrame.Name = "mainFrame";
            this.mainFrame.Size = new System.Drawing.Size(1264, 730);
            this.mainFrame.TabIndex = 3;
            // 
            // btnSnd
            // 
            this.btnSnd.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnSnd.BackColor = System.Drawing.Color.Transparent;
            this.btnSnd.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("btnSnd.BackgroundImage")));
            this.btnSnd.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btnSnd.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSnd.FlatAppearance.BorderSize = 0;
            this.btnSnd.FlatAppearance.MouseDownBackColor = System.Drawing.Color.DarkSlateGray;
            this.btnSnd.FlatAppearance.MouseOverBackColor = System.Drawing.Color.AliceBlue;
            this.btnSnd.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSnd.ForeColor = System.Drawing.Color.White;
            this.btnSnd.Image = ((System.Drawing.Image)(resources.GetObject("btnSnd.Image")));
            this.btnSnd.Location = new System.Drawing.Point(99, 659);
            this.btnSnd.Name = "btnSnd";
            this.btnSnd.Size = new System.Drawing.Size(50, 50);
            this.btnSnd.TabIndex = 6;
            this.btnSnd.UseVisualStyleBackColor = false;
            this.btnSnd.Click += new System.EventHandler(this.btnSnd_Click);
            // 
            // btnWindowed
            // 
            this.btnWindowed.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnWindowed.BackColor = System.Drawing.Color.Transparent;
            this.btnWindowed.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("btnWindowed.BackgroundImage")));
            this.btnWindowed.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btnWindowed.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnWindowed.FlatAppearance.BorderSize = 0;
            this.btnWindowed.FlatAppearance.MouseDownBackColor = System.Drawing.Color.DarkSlateGray;
            this.btnWindowed.FlatAppearance.MouseOverBackColor = System.Drawing.Color.AliceBlue;
            this.btnWindowed.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnWindowed.ForeColor = System.Drawing.Color.White;
            this.btnWindowed.Image = global::ZeroKLobby.Buttons.win_minmax;
            this.btnWindowed.Location = new System.Drawing.Point(25, 659);
            this.btnWindowed.Name = "btnWindowed";
            this.btnWindowed.Size = new System.Drawing.Size(50, 50);
            this.btnWindowed.TabIndex = 5;
            this.btnWindowed.UseVisualStyleBackColor = false;
            this.btnWindowed.Click += new System.EventHandler(this.btnWindowed_Click);
            // 
            // exitButton
            // 
            this.exitButton.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.exitButton.BackColor = System.Drawing.Color.Transparent;
            this.exitButton.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("exitButton.BackgroundImage")));
            this.exitButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.exitButton.Cursor = System.Windows.Forms.Cursors.Hand;
            this.exitButton.FlatAppearance.BorderSize = 0;
            this.exitButton.FlatAppearance.MouseDownBackColor = System.Drawing.Color.DarkSlateGray;
            this.exitButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.AliceBlue;
            this.exitButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.exitButton.ForeColor = System.Drawing.Color.White;
            this.exitButton.Image = ((System.Drawing.Image)(resources.GetObject("exitButton.Image")));
            this.exitButton.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.exitButton.Location = new System.Drawing.Point(25, 431);
            this.exitButton.Name = "exitButton";
            this.exitButton.Size = new System.Drawing.Size(250, 50);
            this.exitButton.TabIndex = 4;
            this.exitButton.Text = "Exit";
            this.exitButton.UseVisualStyleBackColor = false;
            this.exitButton.Click += new System.EventHandler(this.exitButton_Click);
            // 
            // multiplayerButton
            // 
            this.multiplayerButton.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.multiplayerButton.BackColor = System.Drawing.Color.Transparent;
            this.multiplayerButton.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("multiplayerButton.BackgroundImage")));
            this.multiplayerButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.multiplayerButton.Cursor = System.Windows.Forms.Cursors.Hand;
            this.multiplayerButton.FlatAppearance.BorderSize = 0;
            this.multiplayerButton.FlatAppearance.MouseDownBackColor = System.Drawing.Color.DarkSlateGray;
            this.multiplayerButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.AliceBlue;
            this.multiplayerButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.multiplayerButton.ForeColor = System.Drawing.Color.White;
            this.multiplayerButton.Image = ((System.Drawing.Image)(resources.GetObject("multiplayerButton.Image")));
            this.multiplayerButton.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.multiplayerButton.Location = new System.Drawing.Point(25, 327);
            this.multiplayerButton.Name = "multiplayerButton";
            this.multiplayerButton.Size = new System.Drawing.Size(250, 50);
            this.multiplayerButton.TabIndex = 3;
            this.multiplayerButton.Text = "MultiPlayer";
            this.multiplayerButton.UseVisualStyleBackColor = false;
            // 
            // avatarButton
            // 
            this.avatarButton.BackColor = System.Drawing.Color.Transparent;
            this.avatarButton.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("avatarButton.BackgroundImage")));
            this.avatarButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.avatarButton.Cursor = System.Windows.Forms.Cursors.Hand;
            this.avatarButton.FlatAppearance.BorderSize = 0;
            this.avatarButton.FlatAppearance.MouseDownBackColor = System.Drawing.Color.DarkSlateGray;
            this.avatarButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.AliceBlue;
            this.avatarButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.avatarButton.ForeColor = System.Drawing.Color.White;
            this.avatarButton.Image = global::ZeroKLobby.ZklResources.grayuser;
            this.avatarButton.Location = new System.Drawing.Point(25, 25);
            this.avatarButton.Name = "avatarButton";
            this.avatarButton.Size = new System.Drawing.Size(75, 75);
            this.avatarButton.TabIndex = 0;
            this.avatarButton.UseVisualStyleBackColor = false;
            // 
            // singleplayerButton
            // 
            this.singleplayerButton.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.singleplayerButton.BackColor = System.Drawing.Color.Transparent;
            this.singleplayerButton.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("singleplayerButton.BackgroundImage")));
            this.singleplayerButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.singleplayerButton.Cursor = System.Windows.Forms.Cursors.Hand;
            this.singleplayerButton.FlatAppearance.BorderSize = 0;
            this.singleplayerButton.FlatAppearance.MouseDownBackColor = System.Drawing.Color.DarkSlateGray;
            this.singleplayerButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.AliceBlue;
            this.singleplayerButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.singleplayerButton.ForeColor = System.Drawing.Color.White;
            this.singleplayerButton.Image = ((System.Drawing.Image)(resources.GetObject("singleplayerButton.Image")));
            this.singleplayerButton.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.singleplayerButton.Location = new System.Drawing.Point(25, 226);
            this.singleplayerButton.Name = "singleplayerButton";
            this.singleplayerButton.Size = new System.Drawing.Size(250, 50);
            this.singleplayerButton.TabIndex = 2;
            this.singleplayerButton.Text = "Play alone";
            this.singleplayerButton.UseVisualStyleBackColor = false;
            // 
            // WelcomeForm
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.White;
            this.BackgroundImage = global::ZeroKLobby.BgImages.bg_battle;
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.ClientSize = new System.Drawing.Size(1264, 730);
            this.Controls.Add(this.mainFrame);
            this.DoubleBuffered = true;
            this.MinimumSize = new System.Drawing.Size(1024, 768);
            this.Name = "WelcomeForm";
            this.Text = "WelcomeForm";
            this.Load += new System.EventHandler(this.WelcomeForm_Load);
            this.mainFrame.ResumeLayout(false);
            this.mainFrame.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private BitmapButton avatarButton;
        private Label label1;
        private BitmapButton singleplayerButton;
        private Panel mainFrame;
        private BitmapButton multiplayerButton;
        private BitmapButton exitButton;
        private BitmapButton btnWindowed;
        private BitmapButton btnSnd;
    }
}