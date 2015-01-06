using System;
using System.Windows.Forms;

namespace ZeroKLobby
{
    public partial class AcceptAgreementForm: Form
    {
        public string AgreementText
        {
            set
            {
                richTextBox1.Text = value;
                richTextBox1.DeselectAll();
            }
        }

        public AcceptAgreementForm()
        {
            InitializeComponent(); 
        }

        void btnAgree_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

				private void AcceptAgreementForm_Load(object sender, EventArgs e)
				{
                    Icon = ZklResources.ZkIcon;
				}
    }
}
