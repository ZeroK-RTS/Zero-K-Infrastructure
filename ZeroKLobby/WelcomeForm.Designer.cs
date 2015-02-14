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
            this.switchPanel1 = new ZeroKLobby.Controls.SwitchPanel();
            this.btnSnd = new ZeroKLobby.BitmapButton();
            this.btnWindowed = new ZeroKLobby.BitmapButton();
            this.avatarButton = new ZeroKLobby.BitmapButton();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.BackColor = System.Drawing.Color.Transparent;
            this.label1.Font = new System.Drawing.Font("Verdana", 24F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.label1.ForeColor = System.Drawing.Color.White;
            this.label1.Location = new System.Drawing.Point(93, 22);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(227, 38);
            this.label1.TabIndex = 1;
            this.label1.Text = "not logged in";
            // 
            // switchPanel1
            // 
            this.switchPanel1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.switchPanel1.BackColor = System.Drawing.Color.Transparent;
            this.switchPanel1.Location = new System.Drawing.Point(0, 81);
            this.switchPanel1.Name = "switchPanel1";
            this.switchPanel1.Size = new System.Drawing.Size(1264, 581);
            this.switchPanel1.TabIndex = 8;
            // 
            // btnSnd
            // 
            this.btnSnd.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnSnd.BackColor = System.Drawing.Color.Transparent;
            this.btnSnd.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("btnSnd.BackgroundImage")));
            this.btnSnd.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.btnSnd.ButtonStyle = ZeroKLobby.ButtonRenderer.StyleType.DarkHive;
            this.btnSnd.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSnd.FlatAppearance.BorderSize = 0;
            this.btnSnd.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSnd.ForeColor = System.Drawing.Color.White;
            this.btnSnd.Location = new System.Drawing.Point(83, 668);
            this.btnSnd.Name = "btnSnd";
            this.btnSnd.Size = new System.Drawing.Size(50, 50);
            this.btnSnd.SoundType = ZeroKLobby.Controls.SoundPalette.SoundType.Click;
            this.btnSnd.TabIndex = 6;
            this.btnSnd.Text = "blah";
            this.btnSnd.UseVisualStyleBackColor = false;
            this.btnSnd.Click += new System.EventHandler(this.btnSnd_Click);
            // 
            // btnWindowed
            // 
            this.btnWindowed.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnWindowed.BackColor = System.Drawing.Color.Transparent;
            this.btnWindowed.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("btnWindowed.BackgroundImage")));
            this.btnWindowed.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.btnWindowed.ButtonStyle = ZeroKLobby.ButtonRenderer.StyleType.DarkHive;
            this.btnWindowed.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnWindowed.FlatAppearance.BorderSize = 0;
            this.btnWindowed.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnWindowed.ForeColor = System.Drawing.Color.White;
            this.btnWindowed.Location = new System.Drawing.Point(12, 668);
            this.btnWindowed.Name = "btnWindowed";
            this.btnWindowed.Size = new System.Drawing.Size(50, 50);
            this.btnWindowed.SoundType = ZeroKLobby.Controls.SoundPalette.SoundType.Click;
            this.btnWindowed.TabIndex = 5;
            this.btnWindowed.Text = "blah";
            this.btnWindowed.UseVisualStyleBackColor = false;
            this.btnWindowed.Click += new System.EventHandler(this.btnWindowed_Click);
            // 
            // avatarButton
            // 
            this.avatarButton.BackColor = System.Drawing.Color.Transparent;
            this.avatarButton.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("avatarButton.BackgroundImage")));
            this.avatarButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.avatarButton.ButtonStyle = ZeroKLobby.ButtonRenderer.StyleType.DarkHive;
            this.avatarButton.Cursor = System.Windows.Forms.Cursors.Hand;
            this.avatarButton.FlatAppearance.BorderSize = 0;
            this.avatarButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.avatarButton.ForeColor = System.Drawing.Color.White;
            this.avatarButton.Location = new System.Drawing.Point(12, 0);
            this.avatarButton.Name = "avatarButton";
            this.avatarButton.Size = new System.Drawing.Size(75, 75);
            this.avatarButton.SoundType = ZeroKLobby.Controls.SoundPalette.SoundType.Click;
            this.avatarButton.TabIndex = 0;
            this.avatarButton.Text = "blah";
            this.avatarButton.UseVisualStyleBackColor = false;
            // 
            // WelcomeForm
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.White;
            this.BackgroundImage = global::ZeroKLobby.BgImages.bg_battle;
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.ClientSize = new System.Drawing.Size(1264, 730);
            this.Controls.Add(this.btnWindowed);
            this.Controls.Add(this.btnSnd);
            this.Controls.Add(this.avatarButton);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.switchPanel1);
            this.DoubleBuffered = true;
            this.MinimumSize = new System.Drawing.Size(1024, 768);
            this.Name = "WelcomeForm";
            this.Text = "WelcomeForm";
            this.Load += new System.EventHandler(this.WelcomeForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private BitmapButton avatarButton;
        private Label label1;
        private BitmapButton btnWindowed;
        private BitmapButton btnSnd;
        private Controls.SwitchPanel switchPanel1;
    }
}