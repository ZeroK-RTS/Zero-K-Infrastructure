using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using NAudio.Wave;

namespace ZeroKLobby
{
    public partial class WelcomeForm : Form
    {
        WaveOut waveOut;
        AudioFileReader audioReader;

        public WelcomeForm()
        {
            InitializeComponent();
            btnWindowed_Click(this, EventArgs.Empty);
        }

        
        protected override void OnDeactivate(EventArgs e)
        {
            base.OnDeactivate(e);
            this.TopMost = false;
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            if (FormBorderStyle == FormBorderStyle.None) this.TopMost = true;
        }

        private void exitButton_Click(object sender, EventArgs e)
        {
            Program.ShutDown();
        }


        Image resizeStoredBackground;

        protected override void OnResizeBegin(EventArgs e)
        {
            resizeStoredBackground = BackgroundImage;
            BackgroundImage = null;
            base.OnResizeBegin(e);
        }

        protected override void OnResizeEnd(EventArgs e)
        {
            base.OnResizeEnd(e);
            BackgroundImage = resizeStoredBackground;
        }


        private void btnWindowed_Click(object sender, EventArgs e)
        {
            var image = BackgroundImage;
            BackgroundImage = null;
            if (FormBorderStyle == FormBorderStyle.None)
            {
                TopMost = false;
                WindowState = FormWindowState.Normal;
                FormBorderStyle = FormBorderStyle.Sizable;
            }
            else
            {
                FormBorderStyle = FormBorderStyle.None;
                TopMost = true;
                WindowState = FormWindowState.Maximized;
            }
            BackgroundImage = image;
        }

        private void WelcomeForm_Load(object sender, EventArgs e)
        {
            waveOut = new WaveOut();
            audioReader = new AudioFileReader("Rise of the Machines.mp3");
            waveOut.Init(audioReader);
            waveOut.Play();
        }

        private void btnSnd_Click(object sender, EventArgs e)
        {
            if (waveOut.PlaybackState == PlaybackState.Playing) waveOut.Stop();
            else {
                audioReader.Position = 0; 
                waveOut.Play();
            }
        }
    }
}
