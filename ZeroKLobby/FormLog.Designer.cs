namespace ZeroKLobby
{
  partial class FormLog
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
      if (disposing && (components != null)) {
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
        this.tbLog = new System.Windows.Forms.RichTextBox();
        this.SuspendLayout();
        // 
        // tbLog
        // 
        this.tbLog.DetectUrls = false;
        this.tbLog.Dock = System.Windows.Forms.DockStyle.Fill;
        this.tbLog.Location = new System.Drawing.Point(0, 0);
        this.tbLog.Name = "tbLog";
        this.tbLog.ReadOnly = true;
        this.tbLog.Size = new System.Drawing.Size(452, 311);
        this.tbLog.TabIndex = 0;
        this.tbLog.Text = "";
        // 
        // FormLog
        // 
        this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(452, 311);
        this.Controls.Add(this.tbLog);
        this.KeyPreview = true;
        this.MinimizeBox = false;
        this.Name = "FormLog";
        this.Text = "Zero-K lobby log history";
        this.Load += new System.EventHandler(this.FormLog_Load);
        this.VisibleChanged += new System.EventHandler(this.FormLog_VisibleChanged);
        this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FormLog_FormClosing);
        this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.FormLog_KeyDown);
        this.ResumeLayout(false);

    }

    #endregion

    private System.Windows.Forms.RichTextBox tbLog;
  }
}