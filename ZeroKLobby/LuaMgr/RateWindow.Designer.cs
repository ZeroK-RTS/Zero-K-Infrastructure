namespace SpringDownloader.LuaMgr
{
    partial class RateWindow
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(RateWindow));
            this.label1 = new System.Windows.Forms.Label();
            this.buttonSend = new System.Windows.Forms.Button();
            this.labelWidgetName = new System.Windows.Forms.Label();
            this.ratingBarWidget = new XSystem.WinControls.RatingBar();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(43, 14);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(166, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Please give your rating for widget:";
            // 
            // buttonSend
            // 
            this.buttonSend.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonSend.Enabled = false;
            this.buttonSend.Location = new System.Drawing.Point(80, 87);
            this.buttonSend.Name = "buttonSend";
            this.buttonSend.Size = new System.Drawing.Size(93, 23);
            this.buttonSend.TabIndex = 2;
            this.buttonSend.Text = "Send";
            this.buttonSend.UseVisualStyleBackColor = true;
            this.buttonSend.Click += new System.EventHandler(this.buttonSend_Click);
            // 
            // labelWidgetName
            // 
            this.labelWidgetName.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelWidgetName.Location = new System.Drawing.Point(-2, 30);
            this.labelWidgetName.Name = "labelWidgetName";
            this.labelWidgetName.Size = new System.Drawing.Size(254, 29);
            this.labelWidgetName.TabIndex = 3;
            this.labelWidgetName.Text = "Widget Name";
            this.labelWidgetName.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // ratingBarWidget
            // 
            this.ratingBarWidget.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.ratingBarWidget.BackColor = System.Drawing.SystemColors.Control;
            this.ratingBarWidget.BarBackColor = System.Drawing.SystemColors.Control;
            this.ratingBarWidget.Gap = ((byte)(1));
            this.ratingBarWidget.IconEmpty = ((System.Drawing.Image)(resources.GetObject("ratingBarWidget.IconEmpty")));
            this.ratingBarWidget.IconFull = ((System.Drawing.Image)(resources.GetObject("ratingBarWidget.IconFull")));
            this.ratingBarWidget.IconHalf = ((System.Drawing.Image)(resources.GetObject("ratingBarWidget.IconHalf")));
            this.ratingBarWidget.IconsCount = ((byte)(5));
            this.ratingBarWidget.Location = new System.Drawing.Point(77, 57);
            this.ratingBarWidget.Name = "ratingBarWidget";
            this.ratingBarWidget.Rate = 0F;
            this.ratingBarWidget.Size = new System.Drawing.Size(100, 20);
            this.ratingBarWidget.TabIndex = 0;
            this.ratingBarWidget.Text = "ratingBar1";
            this.ratingBarWidget.RateChanged += new XSystem.WinControls.OnRateChanged(this.ratingBarWidget_RateChanged);
            // 
            // RateWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(249, 124);
            this.Controls.Add(this.ratingBarWidget);
            this.Controls.Add(this.labelWidgetName);
            this.Controls.Add(this.buttonSend);
            this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "RateWindow";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Rate Widget";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private XSystem.WinControls.RatingBar ratingBarWidget;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button buttonSend;
        private System.Windows.Forms.Label labelWidgetName;
    }
}