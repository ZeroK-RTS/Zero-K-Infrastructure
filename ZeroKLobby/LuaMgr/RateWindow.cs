using System;
using System.Windows.Forms;
using XSystem.WinControls;

namespace SpringDownloader.LuaMgr
{
    public partial class RateWindow: Form
    {
        Nullable<float> initialRating;
        int nameId;

        public RateWindow(string widgetName, int nameId, Nullable<float> currentUserRating)
        {
            InitializeComponent();

            this.nameId = nameId;
            Text = "Rate Widget - " + widgetName;
            labelWidgetName.Text = widgetName;
            initialRating = currentUserRating;

            if (currentUserRating != null) ratingBarWidget.Rate = currentUserRating.Value;

            buttonSend.Enabled = false;
        }

        void buttonSend_Click(object sender, EventArgs e)
        {
            try
            {
                WidgetHandler.fetcher.addRating(nameId, ratingBarWidget.Rate, Program.Conf.LobbyPlayerName, Program.Conf.LobbyPlayerPassword);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Adding the rating failed: " + ex.Message);
            }
        }

        void ratingBarWidget_RateChanged(object sender, RatingBarRateEventArgs e)
        {
            if (initialRating == null || initialRating.Value != ratingBarWidget.Rate) buttonSend.Enabled = true;
            else buttonSend.Enabled = false;
        }
    }
}