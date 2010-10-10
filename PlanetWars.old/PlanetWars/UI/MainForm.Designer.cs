namespace PlanetWars.UI
{
	partial class MainForm
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
			this.toolStrip1 = new System.Windows.Forms.ToolStrip();
			this.btn_SelectPlanet = new System.Windows.Forms.ToolStripButton();
			this.btn_AddPlanet = new System.Windows.Forms.ToolStripButton();
			this.btn_RemovePlanet = new System.Windows.Forms.ToolStripButton();
			this.btn_AddLink = new System.Windows.Forms.ToolStripButton();
			this.btn_RemoveLink = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.btn_DumpMinimaps = new System.Windows.Forms.ToolStripButton();
			this.btn_SaveGalaxy = new System.Windows.Forms.ToolStripButton();
			this.exportLinksButton = new System.Windows.Forms.ToolStripButton();
			this.btn_SetAllowedMaps = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
			this.clearStateButton = new System.Windows.Forms.ToolStripButton();
			this.simulateButton = new System.Windows.Forms.ToolStripButton();
			this.addPlayerButton = new System.Windows.Forms.ToolStripButton();
			this.battleButton = new System.Windows.Forms.ToolStripButton();
			this.logButton = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
			this.fitToScreenButton = new System.Windows.Forms.ToolStripButton();
			this.panel1 = new System.Windows.Forms.Panel();
			this.exporXmlMapInfoButton = new System.Windows.Forms.ToolStripButton();
			this.toolStrip1.SuspendLayout();
			this.SuspendLayout();
			// 
			// toolStrip1
			// 
			this.toolStrip1.Dock = System.Windows.Forms.DockStyle.Left;
			this.toolStrip1.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.btn_SelectPlanet,
            this.btn_AddPlanet,
            this.btn_RemovePlanet,
            this.btn_AddLink,
            this.btn_RemoveLink,
            this.btn_SetAllowedMaps,
            this.toolStripSeparator1,
            this.btn_DumpMinimaps,
            this.btn_SaveGalaxy,
            this.exportLinksButton,
            this.exporXmlMapInfoButton,
            this.toolStripSeparator3,
            this.clearStateButton,
            this.simulateButton,
            this.addPlayerButton,
            this.battleButton,
            this.logButton,
            this.toolStripSeparator2,
            this.fitToScreenButton});
			this.toolStrip1.LayoutStyle = System.Windows.Forms.ToolStripLayoutStyle.VerticalStackWithOverflow;
			this.toolStrip1.Location = new System.Drawing.Point(0, 0);
			this.toolStrip1.Name = "toolStrip1";
			this.toolStrip1.Padding = new System.Windows.Forms.Padding(0);
			this.toolStrip1.Size = new System.Drawing.Size(129, 459);
			this.toolStrip1.TabIndex = 0;
			this.toolStrip1.Text = "toolStrip1";
			// 
			// btn_SelectPlanet
			// 
			this.btn_SelectPlanet.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			this.btn_SelectPlanet.Image = ((System.Drawing.Image)(resources.GetObject("btn_SelectPlanet.Image")));
			this.btn_SelectPlanet.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btn_SelectPlanet.Name = "btn_SelectPlanet";
			this.btn_SelectPlanet.Size = new System.Drawing.Size(128, 17);
			this.btn_SelectPlanet.Text = "Select Planet";
			this.btn_SelectPlanet.Click += new System.EventHandler(this.btn_SelectPlanet_Click);
			// 
			// btn_AddPlanet
			// 
			this.btn_AddPlanet.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			this.btn_AddPlanet.Image = ((System.Drawing.Image)(resources.GetObject("btn_AddPlanet.Image")));
			this.btn_AddPlanet.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btn_AddPlanet.Name = "btn_AddPlanet";
			this.btn_AddPlanet.Size = new System.Drawing.Size(128, 17);
			this.btn_AddPlanet.Text = "Add Planet";
			this.btn_AddPlanet.Click += new System.EventHandler(this.btn_AddPlanet_Click);
			// 
			// btn_RemovePlanet
			// 
			this.btn_RemovePlanet.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			this.btn_RemovePlanet.Image = ((System.Drawing.Image)(resources.GetObject("btn_RemovePlanet.Image")));
			this.btn_RemovePlanet.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btn_RemovePlanet.Name = "btn_RemovePlanet";
			this.btn_RemovePlanet.Size = new System.Drawing.Size(128, 17);
			this.btn_RemovePlanet.Text = "Remove Planet";
			this.btn_RemovePlanet.Click += new System.EventHandler(this.btn_RemovePlanet_Click);
			// 
			// btn_AddLink
			// 
			this.btn_AddLink.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			this.btn_AddLink.Image = ((System.Drawing.Image)(resources.GetObject("btn_AddLink.Image")));
			this.btn_AddLink.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btn_AddLink.Name = "btn_AddLink";
			this.btn_AddLink.Size = new System.Drawing.Size(128, 17);
			this.btn_AddLink.Text = "Add Link";
			this.btn_AddLink.Click += new System.EventHandler(this.btn_AddLink_Click);
			// 
			// btn_RemoveLink
			// 
			this.btn_RemoveLink.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			this.btn_RemoveLink.Image = ((System.Drawing.Image)(resources.GetObject("btn_RemoveLink.Image")));
			this.btn_RemoveLink.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btn_RemoveLink.Name = "btn_RemoveLink";
			this.btn_RemoveLink.Size = new System.Drawing.Size(128, 17);
			this.btn_RemoveLink.Text = "Remove Link";
			this.btn_RemoveLink.Click += new System.EventHandler(this.btn_RemoveLink_Click);
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(128, 6);
			// 
			// btn_DumpMinimaps
			// 
			this.btn_DumpMinimaps.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			this.btn_DumpMinimaps.Image = ((System.Drawing.Image)(resources.GetObject("btn_DumpMinimaps.Image")));
			this.btn_DumpMinimaps.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btn_DumpMinimaps.Name = "btn_DumpMinimaps";
			this.btn_DumpMinimaps.Size = new System.Drawing.Size(128, 17);
			this.btn_DumpMinimaps.Text = "Dump Maps to C:/Export";
			this.btn_DumpMinimaps.Click += new System.EventHandler(this.btn_DumpMinimaps_Click);
			// 
			// btn_SaveGalaxy
			// 
			this.btn_SaveGalaxy.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			this.btn_SaveGalaxy.Image = ((System.Drawing.Image)(resources.GetObject("btn_SaveGalaxy.Image")));
			this.btn_SaveGalaxy.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btn_SaveGalaxy.Name = "btn_SaveGalaxy";
			this.btn_SaveGalaxy.Size = new System.Drawing.Size(128, 17);
			this.btn_SaveGalaxy.Text = "Save to galaxy.xml";
			this.btn_SaveGalaxy.Click += new System.EventHandler(this.btn_SaveGalaxy_Click);
			// 
			// exportLinksButton
			// 
			this.exportLinksButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			this.exportLinksButton.Image = ((System.Drawing.Image)(resources.GetObject("exportLinksButton.Image")));
			this.exportLinksButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.exportLinksButton.Name = "exportLinksButton";
			this.exportLinksButton.Size = new System.Drawing.Size(128, 17);
			this.exportLinksButton.Text = "Export Link Images";
			this.exportLinksButton.Click += new System.EventHandler(this.exportLinksButton_Click);
			// 
			// btn_SetAllowedMaps
			// 
			this.btn_SetAllowedMaps.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			this.btn_SetAllowedMaps.Image = ((System.Drawing.Image)(resources.GetObject("btn_SetAllowedMaps.Image")));
			this.btn_SetAllowedMaps.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btn_SetAllowedMaps.Name = "btn_SetAllowedMaps";
			this.btn_SetAllowedMaps.Size = new System.Drawing.Size(128, 17);
			this.btn_SetAllowedMaps.Text = "Set Allowed Maps";
			this.btn_SetAllowedMaps.Click += new System.EventHandler(this.btn_SetAllowedMaps_Click);
			// 
			// toolStripSeparator3
			// 
			this.toolStripSeparator3.Name = "toolStripSeparator3";
			this.toolStripSeparator3.Size = new System.Drawing.Size(128, 6);
			// 
			// clearStateButton
			// 
			this.clearStateButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			this.clearStateButton.Image = ((System.Drawing.Image)(resources.GetObject("clearStateButton.Image")));
			this.clearStateButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.clearStateButton.Name = "clearStateButton";
			this.clearStateButton.Size = new System.Drawing.Size(128, 17);
			this.clearStateButton.Text = "Delete Server State";
			this.clearStateButton.Click += new System.EventHandler(this.clearStateButton_Click);
			// 
			// simulateButton
			// 
			this.simulateButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			this.simulateButton.Image = ((System.Drawing.Image)(resources.GetObject("simulateButton.Image")));
			this.simulateButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.simulateButton.Name = "simulateButton";
			this.simulateButton.Size = new System.Drawing.Size(128, 17);
			this.simulateButton.Text = "Simulate Day";
			this.simulateButton.ToolTipText = "Simulate Game";
			this.simulateButton.Click += new System.EventHandler(this.simulateButton_Click);
			// 
			// addPlayerButton
			// 
			this.addPlayerButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			this.addPlayerButton.Image = ((System.Drawing.Image)(resources.GetObject("addPlayerButton.Image")));
			this.addPlayerButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.addPlayerButton.Name = "addPlayerButton";
			this.addPlayerButton.Size = new System.Drawing.Size(128, 17);
			this.addPlayerButton.Text = "Register Player";
			this.addPlayerButton.Click += new System.EventHandler(this.addPlayerButton_Click);
			// 
			// battleButton
			// 
			this.battleButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			this.battleButton.Image = ((System.Drawing.Image)(resources.GetObject("battleButton.Image")));
			this.battleButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.battleButton.Name = "battleButton";
			this.battleButton.Size = new System.Drawing.Size(128, 17);
			this.battleButton.Text = "Battle";
			this.battleButton.Click += new System.EventHandler(this.battleButton_Click);
			// 
			// logButton
			// 
			this.logButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			this.logButton.ForeColor = System.Drawing.SystemColors.ControlText;
			this.logButton.Image = ((System.Drawing.Image)(resources.GetObject("logButton.Image")));
			this.logButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.logButton.Name = "logButton";
			this.logButton.Size = new System.Drawing.Size(128, 17);
			this.logButton.Text = "Print Log to Debug";
			this.logButton.Click += new System.EventHandler(this.logButton_Click);
			// 
			// toolStripSeparator2
			// 
			this.toolStripSeparator2.Name = "toolStripSeparator2";
			this.toolStripSeparator2.Size = new System.Drawing.Size(128, 6);
			// 
			// fitToScreenButton
			// 
			this.fitToScreenButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			this.fitToScreenButton.Image = ((System.Drawing.Image)(resources.GetObject("fitToScreenButton.Image")));
			this.fitToScreenButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.fitToScreenButton.Name = "fitToScreenButton";
			this.fitToScreenButton.Size = new System.Drawing.Size(128, 17);
			this.fitToScreenButton.Text = "Zoom 1:1";
			this.fitToScreenButton.Click += new System.EventHandler(this.fitToScreenButton_Click);
			// 
			// panel1
			// 
			this.panel1.BackColor = System.Drawing.Color.Black;
			this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panel1.Location = new System.Drawing.Point(129, 0);
			this.panel1.Margin = new System.Windows.Forms.Padding(0);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(734, 459);
			this.panel1.TabIndex = 1;
			// 
			// exporXmlMapInfoButton
			// 
			this.exporXmlMapInfoButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			this.exporXmlMapInfoButton.Image = ((System.Drawing.Image)(resources.GetObject("exporXmlMapInfoButton.Image")));
			this.exporXmlMapInfoButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.exporXmlMapInfoButton.Name = "exporXmlMapInfoButton";
			this.exporXmlMapInfoButton.Size = new System.Drawing.Size(128, 17);
			this.exporXmlMapInfoButton.Text = "Export XML Map Info";
			this.exporXmlMapInfoButton.Click += new System.EventHandler(this.exporXmlMapInfoButton_Click);
			// 
			// MainForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(863, 459);
			this.Controls.Add(this.panel1);
			this.Controls.Add(this.toolStrip1);
			this.Name = "MainForm";
			this.ShowIcon = false;
			this.Text = "Planet Wars";
			this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
			this.toolStrip1.ResumeLayout(false);
			this.toolStrip1.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		public System.Windows.Forms.ToolStrip toolStrip1;
		public System.Windows.Forms.ToolStripButton btn_AddPlanet;
		public System.Windows.Forms.ToolStripButton btn_RemovePlanet;
		public System.Windows.Forms.ToolStripButton btn_SelectPlanet;
		public System.Windows.Forms.ToolStripButton btn_AddLink;
		public System.Windows.Forms.ToolStripButton btn_DumpMinimaps;
		public System.Windows.Forms.ToolStripButton btn_SaveGalaxy;
		public System.Windows.Forms.ToolStripButton btn_SetAllowedMaps;
		public System.Windows.Forms.ToolStripButton btn_RemoveLink;
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
		private System.Windows.Forms.ToolStripButton simulateButton;
		private System.Windows.Forms.ToolStripButton addPlayerButton;
		private System.Windows.Forms.ToolStripButton battleButton;
		private System.Windows.Forms.ToolStripButton logButton;
		private System.Windows.Forms.ToolStripButton clearStateButton;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
		private System.Windows.Forms.ToolStripButton fitToScreenButton;
		private System.Windows.Forms.ToolStripButton exportLinksButton;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
		private System.Windows.Forms.ToolStripButton exporXmlMapInfoButton;
	}
}