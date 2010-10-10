using System;
using System.Windows.Forms;

namespace SpringDownloader.LuaMgr
{
    public partial class CommentWindow: Form
    {
        int nameId;

        public CommentWindow(string widgetName, int nameId)
        {
            InitializeComponent();

            this.nameId = nameId;
            Text = "Comments - " + widgetName;

            fillComments();

            if (!LuaMgrTab.checkLobbyInputData(false))
            {
                buttonCommentSend.Enabled = false;
                textBoxAddComment.Enabled = false;

                var txt = "Please insert spring account data into options to comment on widgets!";
                toolTip1.SetToolTip(buttonCommentSend, txt);
                toolTip1.SetToolTip(textBoxAddComment, txt);
                textBoxAddComment.Text = txt;
            }
        }

        void fillComments()
        {
            var comments = WidgetHandler.fetcher.getCommentsByNameId(nameId);
            foreach (var cmt in comments)
            {
                textBoxComments.Text += "by " + cmt.username + " (" + cmt.entry.ToLocalTime() + ")";
                textBoxComments.Text += Environment.NewLine;
                textBoxComments.Text += "================";
                textBoxComments.Text += Environment.NewLine;
                textBoxComments.Text += cmt.comment;
                textBoxComments.Text += Environment.NewLine;
                textBoxComments.Text += Environment.NewLine;
            }
        }

        void buttonCommentSend_Click(object sender, EventArgs e)
        {
            try
            {
                if (textBoxAddComment.Text.Length > 0)
                {
                    WidgetHandler.fetcher.addComment(nameId, textBoxAddComment.Text, Program.Conf.LobbyPlayerName, Program.Conf.LobbyPlayerPassword);
                    DialogResult = DialogResult.OK;
                    Close();
                }
                else MessageBox.Show("Please enter a comment first.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Adding the comment failed: " + ex.Message);
            }
        }
    }
}