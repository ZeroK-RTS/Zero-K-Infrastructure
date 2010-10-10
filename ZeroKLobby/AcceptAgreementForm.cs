using System;
using System.Windows.Forms;

namespace SpringDownloader
{
    public partial class AcceptAgreementForm: Form
    {
        public string AgreementText
        {
            set
            {
                richTextBox1.Rtf = value;
                richTextBox1.DeselectAll();
            }
        }

        public AcceptAgreementForm()
        {
            InitializeComponent();
        }

        void btnAgree_Click(object sender, EventArgs e)
        {
            Close();
        }

				private void AcceptAgreementForm_Load(object sender, EventArgs e)
				{
					Icon = Resources.SpringDownloader;
				}
    }
}