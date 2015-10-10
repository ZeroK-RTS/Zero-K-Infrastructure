using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using LobbyClient;


namespace ZeroKLobby.MicroLobby
{
    public partial class SpringsettingForm: Form
    {
        private Dictionary<string, EngineConfigEntry> settingsOptions;

        public SpringsettingForm() {
            InitializeComponent();
            Icon = ZklResources.ZkIcon;
        }

        private void LinkedLabelClicked(object sender, LinkLabelLinkClickedEventArgs e) {
            var CurrentLink = (LinkLabel)sender;
            CurrentLink.LinkVisited = true;
            Utils.OpenWeb(e.Link.LinkData as string, false);
        }


        private void applyButton_Click(object sender, EventArgs e) {
            doneLabel.Visible = false;
            loadDefaultDone.Visible = false;
            foreach (var kvp in settingsOptions) {
                var controlWithName = panel1.Controls.Find(kvp.Key, false);
                if (controlWithName[0] != null) //CHECK in case the setting wasn't found (ie: when the new setting differ from previous list) then skip this one. Anti-bug.
                {
                    if (kvp.Value.type == "bool") {
                        var checkBoxTick = ((CheckBox)controlWithName[0]).Checked;
                        Program.EngineConfigurator.SetConfigValue(kvp.Key, checkBoxTick ? "1" : "0"); //Reference: SettingTab.cs
                    }
                    else {
                        var textBoxContent = controlWithName[0].Text;
                        Program.EngineConfigurator.SetConfigValue(kvp.Key, textBoxContent);
                    }
                }
            }
            doneLabel.Visible = true; //notify user that Apply operation is successful.
        }

        private void cancelButton_Click(object sender, EventArgs e) {
            Close();
        }

        private void engineDefaultButton_Click(object sender, EventArgs e) {
            doneLabel.Visible = false;
            loadDefaultDone.Visible = false;
            foreach (var kvp in settingsOptions) {
                var controlWithName = panel1.Controls.Find(kvp.Key, false);
                if (controlWithName[0] != null) //CHECK in case the setting wasn't found (ie: when the new setting differ from previous list) then skip this one. Anti-bug.
                {
                    if (kvp.Value.type == "bool") ((CheckBox)controlWithName[0]).Checked = (kvp.Value.defaultValue == "1");
                    else controlWithName[0].Text = kvp.Value.defaultValue;
                }
            }
            loadDefaultDone.Visible = true; //notify user that 'defaulting the list' operation is successful.
        }

        private void SpringsettingForm_Load(object sender, EventArgs e)
        {
            SuspendLayout(); //pause layout until all element is set
            try
            {
                Program.ToolTip.SetText(highlighttextBox, "Highlight any options that contain this term as name");
                Program.ToolTip.SetText(highlightlabel, "Highlighter");

                Program.ToolTip.SetText(engineDefaultButton, "Replace all entries with Spring's default values");
                Program.ToolTip.SetText(cancelButton, "Exit, do not commit change");
                Program.ToolTip.SetText(applyButton, "Write all entries to Springsettings.cfg");
                
                if(!Utils.VerifySpringInstalled())
                {
                	Program.Downloader.GetAndSwitchEngine(Program.SpringPaths.SpringVersion);
                	this.Close();
                    return;
                }

                settingsOptions = new SpringSettings().GetEngineConfigOptions(Program.SpringPaths);

                var location = 0;
                foreach (var kvp in settingsOptions) //ref: http://www.dotnetperls.com/dictionary, http://stackoverflow.com/questions/10556205/deserializing-a-json-with-variable-name-value-pairs-into-object-in-c-sharp
                {
                    var option = kvp.Value;

                    //Create links and label.
                    var NewLinkLabel = new LinkLabel();
                    NewLinkLabel.Text = kvp.Key;

                    var cPlusPlusFile = option.declarationFile;
                    var truncatedCPlusPlusPath = Regex.Match(cPlusPlusFile, @"rts/([a-zA-Z0-9\\~\\!\\@\\#\\$\\%\\^\\&amp;\\*\\(\\)_\\-\\=\\+\\\\\\/\\?\\.\\:\\;\\'\\,]*)?", RegexOptions.IgnoreCase);
                    //Reference: http://www.dotnetperls.com/regex-match, http://weblogs.asp.net/farazshahkhan/archive/2008/08/09/regex-to-find-url-within-text-and-make-them-as-link.aspx

                    var hyperLink = "https://github.com/spring/spring/blob/develop/" + truncatedCPlusPlusPath.Groups[0].Value + "#L" + settingsOptions[kvp.Key].declarationLine;

                    var link = new LinkLabel.Link(); //Reference: http://www.dotnetperls.com/linklabel
                    link.LinkData = hyperLink;

                    NewLinkLabel.LinkClicked += LinkedLabelClicked;
                    NewLinkLabel.Links.Add(0, 250, link.LinkData); //Reference: http://www.c-sharpcorner.com/uploadfile/mahesh/linklabel-in-C-Sharp/
                    NewLinkLabel.Location = new Point(10, location);
                    NewLinkLabel.Size = new Size(250, 17);
                    NewLinkLabel.AccessibleDescription = truncatedCPlusPlusPath.Groups[0].Value + "#L" + settingsOptions[kvp.Key].declarationLine;
                    Program.ToolTip.SetText(NewLinkLabel, "Hyperlink: Spring/" + truncatedCPlusPlusPath.Groups[0].Value + "#L" + settingsOptions[kvp.Key].declarationLine);

                    // Set up/customize the ToolTip text for the Button and Checkbox.
                    var tooltip = "";
                    if (option.description != null) tooltip += string.Format("Description: {0}\n", option.description);
                    if (option.readOnly==1) tooltip += string.Format("ReadOnly: True\n");
                    if (option.defaultValue != null) tooltip += string.Format("DefaultValue: {0}\n", option.defaultValue);
                    if (option.safemodeValue != null) tooltip += string.Format("SafeModeValue: {0}\n", option.safemodeValue);
                    if (option.minimumValue != null) tooltip += string.Format("MinimumValue: {0}\n", option.minimumValue);
                    if (option.maximumValue != null) tooltip += string.Format("MaximumValue: {0}\n", option.maximumValue);

                    if (option.type == "bool") //customized tooltip for boolean entry
                    {
                        //Create checkbox
                        var NewCheckBox = new CheckBox();
                        var presentValue = Program.EngineConfigurator.GetConfigValue(kvp.Key);
                        if (presentValue != null) //retrieve value from Springsetting.cfg
                            NewCheckBox.Checked = presentValue == "1";
                        else NewCheckBox.Checked = option.defaultValue == "1";
                        NewCheckBox.Location = new Point(260, location);
                        NewCheckBox.Size = new Size(200, 17);
                        NewCheckBox.Name = kvp.Key;
                        NewCheckBox.AccessibleDescription = tooltip;

                        //add all the controls
                        panel1.Controls.Add(NewLinkLabel);
                        panel1.Controls.Add(NewCheckBox);

                        Program.ToolTip.SetText(NewCheckBox, tooltip);
                    }
                    else
                    {
                        //Create textbox
                        var NewTextBox = new TextBox();
                        var presentValue = Program.EngineConfigurator.GetConfigValue(kvp.Key);
                        if (presentValue != null) NewTextBox.Text = presentValue;
                        else NewTextBox.Text = option.defaultValue;
                        NewTextBox.Location = new Point(260, location);
                        NewTextBox.Size = new Size(200, 17);
                        NewTextBox.Name = kvp.Key;
                        NewTextBox.AccessibleDescription = tooltip;

                        //add all the controls
                        panel1.Controls.Add(NewLinkLabel);
                        panel1.Controls.Add(NewTextBox);

                        Program.ToolTip.SetText(NewTextBox, tooltip);
                    }

                    //lower next entry position -30 points
                    location = location + 30;
                }
                Icon = ZklResources.ZkIcon;
            }
            catch (Exception ex) {
                ErrorHandling.HandleException(ex, false);
            }
            ResumeLayout();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string filterTerm;
            filterTerm = highlighttextBox.Text.ToLowerInvariant();

            foreach (var kvp in settingsOptions)
            {
                var controlWithName = panel1.Controls.Find(kvp.Key, false);
                if (controlWithName[0] != null) //CHECK in case the setting wasn't found (ie: when the new setting differ from previous list) then skip this one. Anti-bug.
                {
                    string keyNameLowercase = kvp.Key.ToLowerInvariant();
                    if (keyNameLowercase.Contains(filterTerm))
                    {
                        controlWithName[0].BackColor = Color.Aqua;
                    }
                    else
                    {
                        controlWithName[0].BackColor = Color.White;
                    }
                }
            }

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            //Reference3: http://stackoverflow.com/questions/7545775/how-to-loop-through-all-controls-in-a-windows-forms-form-or-how-to-find-if-a-par
            //Reference2: http://stackoverflow.com/questions/12686623/change-cell-background-color-in-winform-datagrid-c-sharp
            //Reference1: http://stackoverflow.com/questions/1845297/how-to-implement-a-search-box-in-c-sharp
            string filterTerm;
            bool zeroEntry=false;
            filterTerm = highlighttextBox.Text.ToLowerInvariant();
            if (filterTerm == "") {
                zeroEntry = true;
            }
            foreach (Control c in panel1.Controls)
            {
                if (zeroEntry)
                { 
                   c.BackColor = Color.White;
                   continue;
                }
                string keyNameLowercase = c.Name.ToLowerInvariant();
                string keyTextLowercase = c.Text.ToLowerInvariant();
                string textAssignedToTooltip = c.AccessibleDescription.ToLowerInvariant();
                if (keyNameLowercase.Contains(filterTerm) || keyTextLowercase.Contains(filterTerm) || textAssignedToTooltip.Contains(filterTerm))
                {
                    c.BackColor = Color.Aqua;
                }
                else
                {
                    c.BackColor = Color.White;
                }
            }
        }

    }
}