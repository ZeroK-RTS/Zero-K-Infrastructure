using System;
using System.Windows.Forms;

namespace ZeroKLobby.MicroLobby
{
    public partial class AskBattlePasswordForm: Form
    {
        public string Password { get { return tbPassword.Text; } }

        public AskBattlePasswordForm(string label)
        {
            InitializeComponent();
            lbTitle.Text = lbTitle.Text + label;
        }


        void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        void btnOk_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

				private void AskBattlePasswordForm_Load(object sender, EventArgs e)
				{
					Icon = Resources.ZkIcon;
				}
    }
}