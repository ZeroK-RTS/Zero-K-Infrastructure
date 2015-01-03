using System;
using System.Windows.Forms;

namespace ZeroKLobby.MicroForms
{
    public static class UnitSyncRetryPrompt
    {
        static PromptForm thisInstance;
        static string rememberedResult;
        static Timer countDown;
        static int counter = 0;

        public static void SpringScanner_RetryGetResourceInfo(object sender, ZkData.CancelEventArgs<ZkData.SpringScanner.CacheItem> e)
        {
            if (thisInstance != null)
                thisInstance.Dispose();

            if (rememberedResult != null)
            {
                e.Cancel = rememberedResult=="cancel";
                return;
            }

            thisInstance = new PromptForm();
            countDown = new Timer();
            countDown.Tick += (s, e1) => { 
                counter++;
                thisInstance.noButton.Text = "No (" + (30 - counter).ToString() + ")";
                if (counter==31)
                    thisInstance.DialogResult = DialogResult.Cancel;
            };
            countDown.Interval = 1000;
            countDown.Enabled = true;
            counter = 0;
            thisInstance.FormClosed += (s, e1) => { countDown.Dispose(); };
            thisInstance.detailBox.Visible = true;
            thisInstance.Text = "New resource found!";
            thisInstance.questionText.Text = Environment.NewLine + "Server connection failed. Extract \"" + e.Data.FileName + "\" information manually?";
            thisInstance.noButton.Text = "No,Wait";
            Program.ToolTip.SetText(thisInstance.okButton, "Perform UnitSync on this file immediately if UnitSync is available");
            Program.ToolTip.SetText(thisInstance.noButton, "Reask server for map/mod information after 2 minute");
            Program.ToolTip.SetText(thisInstance.rememberChoiceCheckbox, "Remember choice for this session only");
            thisInstance.detailText.WordWrap = false;
            var detailText = "File name: " + e.Data.FileName + Environment.NewLine;
            detailText = detailText + "MD5: " + e.Data.Md5.ToString() + Environment.NewLine;
            detailText = detailText + "Internal name: " + e.Data.InternalName + Environment.NewLine;
            detailText = detailText + "Recommended action: Restore Connection & Wait";
            thisInstance.detailText.Text = detailText;
            thisInstance.ShowDialog();

            e.Cancel = (thisInstance.DialogResult == DialogResult.OK);

            if (thisInstance.rememberChoiceCheckbox.Checked)
                rememberedResult = e.Cancel ? "cancel" : "ok";
        }
    }
}
