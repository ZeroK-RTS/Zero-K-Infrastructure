namespace ZeroKLobby
{
    partial class MainWindow
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
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.notifySection1 = new SpringDownloader.Notifications.NotifySection();
            this.navigationControl1 = new ZeroKLobby.NavigationControl();
            this.tableLayoutPanel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.ColumnCount = 1;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.Controls.Add(this.notifySection1, 0, 1);
            this.tableLayoutPanel2.Controls.Add(this.navigationControl1, 0, 0);
            this.tableLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel2.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 2;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel2.Size = new System.Drawing.Size(668, 304);
            this.tableLayoutPanel2.TabIndex = 0;
            // 
            // notifySection1
            // 
            this.notifySection1.AutoSize = true;
            this.notifySection1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.notifySection1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.notifySection1.Location = new System.Drawing.Point(3, 301);
            this.notifySection1.Name = "notifySection1";
            this.notifySection1.Size = new System.Drawing.Size(662, 1);
            this.notifySection1.TabIndex = 0;
            // 
            // navigationControl1
            // 
            this.navigationControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.navigationControl1.Location = new System.Drawing.Point(3, 3);
            this.navigationControl1.Name = "navigationControl1";
            this.navigationControl1.Size = new System.Drawing.Size(662, 292);
            this.navigationControl1.TabIndex = 1;
            // 
            // MainWindow
            // 
            this.ClientSize = new System.Drawing.Size(668, 304);
            this.Controls.Add(this.tableLayoutPanel2);
            this.Name = "MainWindow";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainWindow_FormClosing);
            this.Load += new System.EventHandler(this.MainWindow_Load);
            this.SizeChanged += new System.EventHandler(this.Window_StateChanged);
            this.tableLayoutPanel2.ResumeLayout(false);
            this.tableLayoutPanel2.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private SpringDownloader.Notifications.NotifySection notifySection1;
        private NavigationControl navigationControl1;
    }
}