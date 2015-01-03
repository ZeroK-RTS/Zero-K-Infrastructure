using System;
using System.Windows.Forms;

namespace ZeroKLobby.MicroForms
{
    public static class UnitSyncUploadPrompt
    {
        static PromptForm thisInstance;
        static string rememberedResult;
        static Timer countDown;
        static int counter = 0;

        public static void SpringScanner_UploadUnitsyncData(object sender, ZkData.CancelEventArgs<ZkData.IResourceInfo> e)
        {
            if (thisInstance != null)
                thisInstance.Dispose();

            if (rememberedResult != null)
            {
                e.Cancel = rememberedResult == "cancel";
                return;
            }

            thisInstance = new PromptForm();
            countDown = new Timer();
            countDown.Tick += (s, e1) => { 
                counter++;
                thisInstance.okButton.Text = "Ok (" + (30 - counter).ToString() + ")";
                if (counter == 31)
                    thisInstance.DialogResult = DialogResult.OK;
            };
            countDown.Interval = 1000;
            countDown.Enabled = true;
            counter = 0;
            thisInstance.FormClosed += (s, e1) => { countDown.Dispose(); };
            thisInstance.detailBox.Visible = true;
            thisInstance.Text = "New information extracted!";
            thisInstance.questionText.Text = Environment.NewLine + "No server data regarding this file hash. Upload \"" + e.Data.Name + "\" information to server?";
            thisInstance.okButton.Text = "Ok,Share";
            Program.ToolTip.SetText(thisInstance.okButton, "This map/mod will be listed on both ZKL and Springie");
            Program.ToolTip.SetText(thisInstance.noButton, "This map/mod will be listed only on this ZKL");
            Program.ToolTip.SetText(thisInstance.rememberChoiceCheckbox, "Remember choice for this session only");
            thisInstance.detailText.WordWrap = false;
            var detailText = "Resource name: " + e.Data.Name + Environment.NewLine;
            detailText = detailText + "File checksum: " + e.Data.Checksum + Environment.NewLine;
            detailText = detailText + "Archive name: " + e.Data.ArchiveName + Environment.NewLine;
            detailText = detailText + "Recommended action: Share multiplayer resource";
            thisInstance.detailText.Text = detailText;
            thisInstance.ShowDialog();

            e.Cancel = (thisInstance.DialogResult == DialogResult.Cancel);

            if (thisInstance.rememberChoiceCheckbox.Checked)
                rememberedResult = e.Cancel ? "cancel" : "ok";
        }
    }
}
