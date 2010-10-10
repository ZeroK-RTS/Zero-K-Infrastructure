using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using LuaManagerLib;
using SpringDownloader;

//using System.Linq;

namespace SpringDownloader
{
    public class ImageGallery: Form
    {
        Button button1;
        Button button2;
        int curIndex;
        FileInfo currentImg;
        List<FileInfo> imageDb;
        Label label1;
        PictureBox pictureBox1;

        public ImageGallery(LinkedList<FileInfo> inDb)
        {
            InitializeComponent();

            imageDb = new List<FileInfo>(inDb);

            updateDisplay();
        }

        void button1_Click(object sender, EventArgs e) {}

        void InitializeComponent()
        {
            label1 = new Label();
            button2 = new Button();
            button1 = new Button();
            pictureBox1 = new PictureBox();
            ((ISupportInitialize)(pictureBox1)).BeginInit();
            SuspendLayout();
            // 
            // label1
            // 
            label1.Anchor = AnchorStyles.Bottom;
            label1.AutoSize = true;
            label1.Font = new Font("Microsoft Sans Serif", 12F, FontStyle.Bold, GraphicsUnit.Point, ((0)));
            label1.Location = new Point(311, 412);
            label1.Name = "label1";
            label1.Size = new Size(34, 20);
            label1.TabIndex = 7;
            label1.Text = "4/4";
            label1.TextAlign = ContentAlignment.BottomCenter;
            // 
            // button2
            // 
            button2.Anchor = AnchorStyles.Bottom;
            button2.Font = new Font("Microsoft Sans Serif", 14.25F, FontStyle.Regular, GraphicsUnit.Point, ((0)));
            button2.Location = new Point(373, 409);
            button2.Name = "button2";
            button2.Size = new Size(96, 28);
            button2.TabIndex = 6;
            button2.Text = ">";
            button2.UseVisualStyleBackColor = true;
            button2.Click += button2_Click_1;
            // 
            // button1
            // 
            button1.Anchor = AnchorStyles.Bottom;
            button1.Font = new Font("Microsoft Sans Serif", 14.25F, FontStyle.Regular, GraphicsUnit.Point, ((0)));
            button1.Location = new Point(189, 409);
            button1.Name = "button1";
            button1.Size = new Size(96, 28);
            button1.TabIndex = 5;
            button1.Text = "<";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click_1;
            // 
            // pictureBox1
            // 
            pictureBox1.Anchor = (((((AnchorStyles.Top | AnchorStyles.Bottom) | AnchorStyles.Left) | AnchorStyles.Right)));
            pictureBox1.BorderStyle = BorderStyle.Fixed3D;
            pictureBox1.Location = new Point(2, 2);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(675, 401);
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox1.TabIndex = 4;
            pictureBox1.TabStop = false;
            // 
            // ImageGallery
            // 
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            ClientSize = new Size(679, 442);
            Controls.Add(label1);
            Controls.Add(button2);
            Controls.Add(button1);
            Controls.Add(pictureBox1);
            FormBorderStyle = FormBorderStyle.SizableToolWindow;
            Name = "ImageGallery";
            ShowIcon = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Image Gallery";
            ((ISupportInitialize)(pictureBox1)).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        void moveNext()
        {
            if ((curIndex + 1) < imageDb.Count)
            {
                curIndex++;
                updateDisplay();
            }
        }

        void movePrev()
        {
            if (curIndex > 0)
            {
                curIndex--;
                updateDisplay();
            }
        }

        protected void updateDisplay()
        {
            currentImg = imageDb[curIndex];

            label1.Text = (curIndex + 1) + "/" + imageDb.Count;

            if (curIndex == 0) button1.Visible = false;
            else button1.Visible = true;

            if ((curIndex + 1) < imageDb.Count) button2.Visible = true;
            else button2.Visible = false;

            pictureBox1.ImageLocation = currentImg.Url;
            pictureBox1.LoadAsync();
        }

        void button1_Click_1(object sender, EventArgs e)
        {
            movePrev();
        }

        void button2_Click_1(object sender, EventArgs e)
        {
            moveNext();
        }
    }
}