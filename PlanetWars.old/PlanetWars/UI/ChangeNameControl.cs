using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace PlanetWars.UI
{
    public partial class ChangeNameControl : UserControl
    {
        readonly string[] names;

        public ChangeNameControl()
        {
            var g = GalaxyMap.Instance.Galaxy;
            InitializeComponent();
            base.Dock = DockStyle.Fill;
            names = g.Planets.Select(p => p.Name).ToArray();
            textBox1.Text = g.GetPlanet(Program.AuthInfo.Login).Name;
            textBox1.TextChanged += textBox1_TextChanged;
            button1.Click += button1_Click;
            
        }

        void button1_Click(object sender, EventArgs e)
        {
        	string result;
        	Program.Server.ChangePlanetName(textBox1.Text, Program.AuthInfo, out result);
            new LoadingForm().ShowDialog();
        }

        void textBox1_TextChanged(object sender, EventArgs e)
        {
			textBox1.BackColor = Color.LightPink;
			button1.Enabled = false;
			if (names.Contains(textBox1.Text))
			{
				ErrorText.Text = "Name already taken";
			} 
			else if (textBox1.Text.Length < 3)
			{
				ErrorText.Text = "Name is too short";
			}
			else if (textBox1.Text.Length > 15)
			{
				ErrorText.Text = "Name is too long";
			}
			else
			{
				ErrorText.Text = "";
				textBox1.BackColor = Color.White;
				button1.Enabled = true;
			}
        }
    }
}