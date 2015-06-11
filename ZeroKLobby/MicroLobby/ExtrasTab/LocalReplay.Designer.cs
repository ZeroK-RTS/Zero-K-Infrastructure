namespace ZeroKLobby.MicroLobby.ExtrasTab
{
	partial class LocalReplay
	{
		/// <summary>
		/// Designer variable used to keep track of non-visual components.
		/// </summary>
		private System.ComponentModel.IContainer components = null;
		private System.Windows.Forms.ListBox listBoxDemoList;
		private ZeroKLobby.BitmapButton btnLaunch;
		private System.Windows.Forms.Panel panel1;
		private ZeroKLobby.BitmapButton buttonRefresh;
		private System.Windows.Forms.Label label1;
		
		/// <summary>
		/// Disposes resources used by the control.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing) {
				if (components != null) {
					components.Dispose();
				}
			}
			base.Dispose(disposing);
		}
		
		/// <summary>
		/// This method is required for Windows Forms designer support.
		/// Do not change the method contents inside the source code editor. The Forms designer might
		/// not be able to load this method if it was changed manually.
		/// </summary>
		private void InitializeComponent()
		{
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LocalReplay));
            this.panel1 = new System.Windows.Forms.Panel();
            this.label1 = new System.Windows.Forms.Label();
            this.buttonRefresh = new ZeroKLobby.BitmapButton();
            this.listBoxDemoList = new System.Windows.Forms.ListBox();
            this.btnLaunch = new ZeroKLobby.BitmapButton();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.label1);
            this.panel1.Controls.Add(this.buttonRefresh);
            this.panel1.Controls.Add(this.listBoxDemoList);
            this.panel1.Controls.Add(this.btnLaunch);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(512, 320);
            this.panel1.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.Location = new System.Drawing.Point(372, 10);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(137, 250);
            this.label1.TabIndex = 53;
            this.label1.Text = "Select Replay\r\n\r\nShortcut Keys:\r\nDelete - to delete selected\r\nArrow Keys - to sel" +
    "ect";
            // 
            // buttonRefresh
            // 
            this.buttonRefresh.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonRefresh.BackColor = System.Drawing.Color.Transparent;
            this.buttonRefresh.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("editTeamButton.BackgroundImage")));
            this.buttonRefresh.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.buttonRefresh.ButtonStyle = ZeroKLobby.FrameBorderRenderer.StyleType.DarkHive;
            this.buttonRefresh.Cursor = System.Windows.Forms.Cursors.Hand;
            this.buttonRefresh.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonRefresh.ForeColor = System.Drawing.Color.White;
            this.buttonRefresh.Location = new System.Drawing.Point(413, 263);
            this.buttonRefresh.Margin = new System.Windows.Forms.Padding(0);
            this.buttonRefresh.Name = "buttonRefresh";
            this.buttonRefresh.Size = new System.Drawing.Size(96, 22);
            this.buttonRefresh.SoundType = ZeroKLobby.Controls.SoundPalette.SoundType.Click;
            this.buttonRefresh.TabIndex = 52;
            this.buttonRefresh.Text = "Refresh";
            this.buttonRefresh.UseVisualStyleBackColor = true;
            this.buttonRefresh.Click += new System.EventHandler(this.ButtonRefreshClick);
            // 
            // listBoxDemoList
            // 
            this.listBoxDemoList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.listBoxDemoList.FormattingEnabled = true;
            this.listBoxDemoList.IntegralHeight = false;
            this.listBoxDemoList.Location = new System.Drawing.Point(3, 0);
            this.listBoxDemoList.Name = "listBoxDemoList";
            this.listBoxDemoList.Size = new System.Drawing.Size(363, 317);
            this.listBoxDemoList.TabIndex = 50;
            this.listBoxDemoList.MouseUp += new System.Windows.Forms.MouseEventHandler(this.ListBoxDemoListMouseUp_Event);
            // 
            // btnLaunch
            // 
            this.btnLaunch.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnLaunch.Location = new System.Drawing.Point(413, 292);
            this.btnLaunch.Name = "btnLaunch";
            this.btnLaunch.Size = new System.Drawing.Size(96, 25);
            this.btnLaunch.TabIndex = 51;
            this.btnLaunch.Text = "Launch Replay";
            this.btnLaunch.UseVisualStyleBackColor = true;
            this.btnLaunch.Click += new System.EventHandler(this.BtnLaunchClick);
            // 
            // LocalReplay
            // 
            this.BackColor = System.Drawing.Color.DimGray;
            this.ForeColor = System.Drawing.Color.White;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.Controls.Add(this.panel1);
            this.Name = "LocalReplay";
            this.Size = new System.Drawing.Size(512, 320);
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);

		}
	}
}
