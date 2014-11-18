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
    public static class UnitSyncRetryPrompt
    {
        static PromptForm thisInstance;
        static string rememberedResult;

        public static void SpringScanner_RetryGetResourceInfo(object sender, PlasmaShared.CancelEventArgs<PlasmaShared.SpringScanner.CacheItem> e)
        {
            if (rememberedResult != null)
            {
                e.Cancel = rememberedResult=="cancel";
                return;
            }

            var springPath = Program.SpringPaths;
            if (springPath.UnitSyncDirectory == "") //if never set Spring path yet
            {
                var defaultEnginePath = Utils.MakePath(springPath.WritableDirectory, "engine", ZkData.GlobalConst.DefaultEngineOverride);
                springPath.SetEnginePath(defaultEnginePath); //DefaultEngineOverride at PlasmaShared/GlobalConst.cs
            }

            if (thisInstance!=null) thisInstance.Dispose();
            thisInstance = new PromptForm();
            thisInstance.detailBox.Visible = true;
            thisInstance.Text = "New resource found!";
            thisInstance.questionText.Text = Environment.NewLine + "Server connection failed. Extract \"" + e.Data.FileName + "\" information manually?";
            thisInstance.noButton.Text = "No,Wait";
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
