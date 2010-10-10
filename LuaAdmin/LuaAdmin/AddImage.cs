using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using System.IO;
using System.Drawing.Imaging;

namespace LuaAdmin
{
    public partial class AddImageForm : Form
    {
        protected int nameId;
        bool thumbnail;
        protected Size ThumbNailSize = new Size(120, 100);


        public AddImageForm(int nameId, bool thumbnail)
        {
            InitializeComponent();

            this.thumbnail = thumbnail;
            this.nameId = nameId;

            if (thumbnail)
            {
                this.resizeText.Visible = true;
            }
            else
            {
                this.resizeText.Visible = false;
            }
        }

        public void GenerateThumbNail(string sOrgFileName, string sThumbNailFileName, ImageFormat oFormat)
        {
                System.Drawing.Image oImg = System.Drawing.Image.FromFile(sOrgFileName);

                System.Drawing.Image oThumbNail = new Bitmap(this.ThumbNailSize.Width, this.ThumbNailSize.Height, oImg.PixelFormat);

                Graphics oGraphic = Graphics.FromImage(oThumbNail);

                oGraphic.CompositingQuality = CompositingQuality.HighQuality;

                oGraphic.SmoothingMode = SmoothingMode.HighQuality;

                oGraphic.InterpolationMode = InterpolationMode.HighQualityBicubic;

                Rectangle oRectangle = new Rectangle(0, 0, this.ThumbNailSize.Width, this.ThumbNailSize.Height);

                oGraphic.DrawImage(oImg, oRectangle);

                oThumbNail.Save(sThumbNailFileName, oFormat);

                oImg.Dispose();
        }

        private void buttonUpload_Click(object sender, EventArgs e)
        {
            if (textBox1.Text.Length == 0 )
            {
                MessageBox.Show("Missing Data!");
            }

            try
            {
                if (!thumbnail)
                {
                    Program.fetcher.addImage(this.nameId, textBox1.Text);
                }
                else
                {
                    //generate 120x100 thumbnail from image
                    string tempFile = Path.GetTempFileName();
                    this.GenerateThumbNail(textBox1.Text, tempFile, getImageFormatFromFilename(textBox1.Text));
                    Program.fetcher.addThumb(this.nameId, tempFile );
                    File.Delete(tempFile);
                }

                MessageBox.Show("Image added successfully!");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Image upload failed!\nError: " + ex.Message, "Error");
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        static ImageFormat getImageFormatFromFilename(string filename)
        {
            ImageFormat fmt;
            string ext = Path.GetExtension(filename).ToLower();
            if (ext == ".bmp")
            {
                fmt = ImageFormat.Bmp;
            }
            else if ( (ext == ".jpg") || (ext == ".jpeg") )
            {
                fmt = ImageFormat.Jpeg;
            }
            else if (ext == ".png")
            {
                fmt = ImageFormat.Png;
            }
            else if (ext == ".gif")
            {
                fmt = ImageFormat.Gif;
            }
            else if (ext == ".ico")
            {
                fmt = ImageFormat.Icon;
            }
            else if ((ext == ".tif") || (ext == ".tiff"))
            {
                fmt = ImageFormat.Gif;
            }
            else
            {
                throw new ArgumentException("Image format not supported", "filename");
            }

            return fmt;
        }

        private void buttonBrowse_Click(object sender, EventArgs e)
        {
            OpenFileDialog fa = new OpenFileDialog();
            fa.Filter = "Image files (*.bmp,*.gif,*.ico,*.jpg,*.jpeg,*.png,*.tif,*.tiff)|*.bmp;*.gif;*.ico;*.jpg;*.jpeg;*.png;*.tif;*.tiff";

            if (fa.ShowDialog() == DialogResult.OK)
            {
                this.textBox1.Text = fa.FileName;
                this.pictureBox1.ImageLocation = fa.FileName;
            }
        }
    }
}
