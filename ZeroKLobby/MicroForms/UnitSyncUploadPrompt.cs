using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ZeroKLobby.MicroForms
{
    public static class UnitSyncUploadPrompt
    {
        static PromptForm thisInstance;
        static string rememberedResult;

        public static void SpringScanner_UploadUnitsyncData(object sender, PlasmaShared.CancelEventArgs<PlasmaShared.IResourceInfo> e)
        {
            if (rememberedResult != null)
            {
                e.Cancel = rememberedResult == "cancel";
                return;
            }

            if (thisInstance != null) thisInstance.Dispose();
            thisInstance = new PromptForm();
            thisInstance.detailBox.Visible = true;
            thisInstance.Text = "New information extracted!";
            thisInstance.questionText.Text = Environment.NewLine + "No server data regarding this file hash. Upload \"" + e.Data.Name + "\" information to server?";
            thisInstance.okButton.Text = "Ok,Share";
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
