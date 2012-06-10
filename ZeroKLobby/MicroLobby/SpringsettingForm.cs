using System;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
//using Newtonsoft.Json; //Download: http://json.codeplex.com/
using System.Collections.Generic; //Allow to create Dictionary. Ref:http://www.dotnetperls.com/dictionary
using System.Text.RegularExpressions;

namespace ZeroKLobby.MicroLobby
{
    public partial class SpringsettingForm : Form
    {

        public SpringsettingForm()
        {
            InitializeComponent();

            //write tooltip for buttons
            ToolTip toolTipButton1 = new ToolTip();
            ToolTip toolTipButton2 = new ToolTip();
            ToolTip toolTipButton3 = new ToolTip();
            // Set up the delays for the ToolTip.
            toolTipButton1.AutoPopDelay = 0; toolTipButton2.AutoPopDelay = 0; toolTipButton3.AutoPopDelay = 0;
            toolTipButton1.InitialDelay = 0; toolTipButton2.InitialDelay = 0; toolTipButton3.InitialDelay = 0;
            toolTipButton1.ReshowDelay = 0; toolTipButton3.ReshowDelay = 0; toolTipButton2.ReshowDelay = 0;
            // Force the ToolTip text to be displayed whether or not the form is active.
            toolTipButton1.ShowAlways = true; toolTipButton2.ShowAlways = true; toolTipButton3.ShowAlways = true;
            toolTipButton1.SetToolTip(engineDefaultButton, "Replace all entry with default value");
            toolTipButton2.SetToolTip(cancelButton, "Exit, do not commit change");
            toolTipButton3.SetToolTip(applyButton, "Write all entry to Springsetting.cfg");

            LobbyClient.Spring spring = new LobbyClient.Spring(Program.SpringPaths);
            var setting_dictionary = spring.GetEngineConfigOptions();
 
            int location = 0;
            foreach (var value in setting_dictionary) //ref: http://www.dotnetperls.com/dictionary, http://stackoverflow.com/questions/10556205/deserializing-a-json-with-variable-name-value-pairs-into-object-in-c-sharp
            {
                //Create links and label.
                LinkLabel NewLinkLabel = new LinkLabel();
                NewLinkLabel.Text = value.Key;

                string cPlusPlusFile = setting_dictionary[value.Key].declarationFile;
                Match truncatedCPlusPlusPath = Regex.Match(cPlusPlusFile, @"build/([a-zA-Z0-9\\~\\!\\@\\#\\$\\%\\^\\&amp;\\*\\(\\)_\\-\\=\\+\\\\\\/\\?\\.\\:\\;\\'\\,]*)?", RegexOptions.IgnoreCase); //Reference: http://www.dotnetperls.com/regex-match, http://weblogs.asp.net/farazshahkhan/archive/2008/08/09/regex-to-find-url-within-text-and-make-them-as-link.aspx

                string hyperLink = "https://github.com/spring/spring/blob/develop/" + truncatedCPlusPlusPath.Groups[1].Value + "#" + setting_dictionary[value.Key].declarationLine;
                
                LinkLabel.Link link = new LinkLabel.Link(); //Reference: http://www.dotnetperls.com/linklabel
                link.LinkData = hyperLink;
                
                NewLinkLabel.LinkClicked += new LinkLabelLinkClickedEventHandler(LinkedLabelClicked);
                NewLinkLabel.Links.Add(0, 250, link.LinkData); //Reference: http://www.c-sharpcorner.com/uploadfile/mahesh/linklabel-in-C-Sharp/
                NewLinkLabel.Location = new System.Drawing.Point(10, location);
                NewLinkLabel.Size = new System.Drawing.Size(250, 17);
                //------
                // Create the ToolTip and associate with the Form container. Reference: http://msdn.microsoft.com/en-us/library/system.windows.forms.tooltip.settooltip.aspx
                ToolTip toolTip1 = new ToolTip();
                // Set up the delays for the ToolTip.
                toolTip1.AutoPopDelay = 0;
                toolTip1.InitialDelay = 0;
                toolTip1.ReshowDelay = 0;
                // Force the ToolTip text to be displayed whether or not the form is active.
                toolTip1.ShowAlways = true;

                // Set up/customize the ToolTip text for the Button and Checkbox.
                string description = setting_dictionary[value.Key].description;
                string defaultValue = setting_dictionary[value.Key].defaultValue;
                double? minValue = setting_dictionary[value.Key].minimumValue;
                double? maxValue = setting_dictionary[value.Key].maximumValue;
                string safeModeValue = setting_dictionary[value.Key].safemodeValue;
                string minimumValue = null;
                string maximumValue = null;
                if (description != null)
                {
                    description = "Description: " + description;
                }
                else { description = "Description:"; }

                if (setting_dictionary[value.Key].type == "bool")
                {
                    if (defaultValue != null)
                    {
                        defaultValue = "\nDefaultValue: " + (defaultValue == "1" ? "True" : "False"); //Reference: SettingTab.cs
                    }
                    else { defaultValue = "\nDefaultValue:"; }
                    if (safeModeValue != null)
                    {
                        safeModeValue = "\nSafeModeValue: " + (safeModeValue == "1" ? "True" : "False");
                    }
                }
                else
                {
                    if (defaultValue != null)
                    {
                        defaultValue = "\nDefaultValue: " + defaultValue;
                    }
                    else { defaultValue = "\nDefaultValue:"; }
                    if (minValue != null)
                    {
                        minimumValue = "\nMinimumValue: " + (setting_dictionary[value.Key].minimumValue).ToString();
                    }
                    else { minimumValue = "\nMinimumValue:"; }
                    if (maxValue != null)
                    {
                        maximumValue = "\nMaximumValue: " + (setting_dictionary[value.Key].maximumValue).ToString();
                    }
                    else { maximumValue = "\nMaximumValue:"; }
                    if (safeModeValue != null)
                    {
                        safeModeValue = "\nSafeModeValue: " + (safeModeValue);
                    }
                }


                if (setting_dictionary[value.Key].type == "bool") //customized tooltip for boolean entry
                {
                    //Create checkbox
                    CheckBox NewCheckBox = new CheckBox();
                    string presentValue = Program.EngineConfigurator.GetConfigValue(value.Key);
                    if (presentValue != null) //retrieve value from Springsetting.cfg
                    {
                        NewCheckBox.Checked = presentValue == "1";
                    }
                    else
                    {
                        NewCheckBox.Checked = setting_dictionary[value.Key].defaultValue == "1";
                    }
                    NewCheckBox.Click += new EventHandler(NewButton_Click);
                    NewCheckBox.Location = new System.Drawing.Point(260, location);
                    NewCheckBox.Size = new System.Drawing.Size(200, 17);
                    NewCheckBox.Name = value.Key;

                    //add all the controls
                    //this.Controls.Add(NewLinkLabel);
                    //this.Controls.Add(NewTextBox);
                    panel1.Controls.Add(NewLinkLabel);
                    panel1.Controls.Add(NewCheckBox);

                    toolTip1.SetToolTip(NewLinkLabel, string.Format("{0}{1}{2}{3}{4}", description, defaultValue, minimumValue, maximumValue,safeModeValue));
                    toolTip1.SetToolTip(NewCheckBox, string.Format("{0}{1}{2}{3}{4}", description, defaultValue, minimumValue, maximumValue,safeModeValue));
                }
                else
                {
                    //Create textbox
                    TextBox NewTextBox = new TextBox();
                    string presentValue = Program.EngineConfigurator.GetConfigValue(value.Key);
                    if (presentValue != null)
                    {
                        NewTextBox.Text = presentValue;
                    }
                    else
                    {
                        NewTextBox.Text = setting_dictionary[value.Key].defaultValue;
                    }
                    NewTextBox.Click += new EventHandler(NewButton_Click);
                    NewTextBox.Location = new System.Drawing.Point(260, location);
                    NewTextBox.Size = new System.Drawing.Size(200, 17);
                    NewTextBox.Name = value.Key;

                    //add all the controls
                    //this.Controls.Add(NewLinkLabel);
                    //this.Controls.Add(NewTextBox);
                    panel1.Controls.Add(NewLinkLabel);
                    panel1.Controls.Add(NewTextBox);

                    if (setting_dictionary[value.Key].type == "std::string") //customized tooltip for string entry
                    {
                        toolTip1.SetToolTip(NewLinkLabel, string.Format("{0}{1}{4}", description, defaultValue, minimumValue, maximumValue, safeModeValue));
                        toolTip1.SetToolTip(NewTextBox, string.Format("{0}{1}{4}", description, defaultValue, minimumValue, maximumValue, safeModeValue));
                    }
                    else
                    {
                        toolTip1.SetToolTip(NewLinkLabel, string.Format("{0}{1}{2}{3}{4}", description, defaultValue, minimumValue, maximumValue, safeModeValue));
                        toolTip1.SetToolTip(NewTextBox, string.Format("{0}{1}{2}{3}{4}", description, defaultValue, minimumValue, maximumValue, safeModeValue));
                    }
                }

                //lower next entry position -30 points
                location = location + 30;
                //Console.WriteLine(value.Key);
            //this.panel1.Controls.Add(this.tbResx);

            //this.tbResx.Location = new System.Drawing.Point(251, 69);
            //this.tbResx.Margin = new System.Windows.Forms.Padding(4);
            //this.tbResx.Name = "tbResx";
            //this.tbResx.Size = new System.Drawing.Size(83, 22);
            //this.tbResx.TabIndex = 28;
            //this.tbResx.TextChanged += new System.EventHandler(this.settingsControlChanged);

            //private System.Windows.Forms.TextBox tbResx;
            }

        }
        void NewButton_Click(object sender, EventArgs e)
        {
            Button CurrentButton = (Button)sender;
            CurrentButton.Text = "I was clicked";
        }
        void LinkedLabelClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            LinkLabel CurrentLink = (LinkLabel)sender;
            CurrentLink.LinkVisited = true;
            System.Diagnostics.Process.Start(e.Link.LinkData as String);     

        }

        private void applyButton_Click(object sender, EventArgs e)
        {
            doneLabel.Visible = false;
            loadDefaultDone.Visible = false;
            LobbyClient.Spring spring = new LobbyClient.Spring(Program.SpringPaths);
            var setting_dictionary = spring.GetEngineConfigOptions(); //get new list.
            foreach (var value in setting_dictionary)
            {
                Control[] controlWithName = panel1.Controls.Find(value.Key, false);
                if (controlWithName[0] != null) //CHECK in case the setting wasn't found (ie: when the new setting differ from previous list) then skip this one. Anti-bug.
                {
                    if (setting_dictionary[value.Key].type == "bool")
                    {
                        bool checkBoxTick = ((CheckBox)controlWithName[0]).Checked;
                        Program.EngineConfigurator.SetConfigValue(value.Key, checkBoxTick ? "1" : "0"); //Reference: SettingTab.cs
                    }
                    else
                    {
                        string textBoxContent = controlWithName[0].Text;
                        Program.EngineConfigurator.SetConfigValue(value.Key, textBoxContent);
                    }
                }
                
            }
            doneLabel.Visible = true; //notify user that Apply operation is successful.
        }

        private void engineDefaultButton_Click(object sender, EventArgs e)
        {
            doneLabel.Visible = false; 
            loadDefaultDone.Visible = false;
            LobbyClient.Spring spring = new LobbyClient.Spring(Program.SpringPaths);
            var setting_dictionary = spring.GetEngineConfigOptions(); //get new list.
            foreach (var value in setting_dictionary)
            {
                Control[] controlWithName = panel1.Controls.Find(value.Key, false);
                if (controlWithName[0] != null) //CHECK in case the setting wasn't found (ie: when the new setting differ from previous list) then skip this one. Anti-bug.
                {
                    if (setting_dictionary[value.Key].type == "bool")
                    {
                        ((CheckBox)controlWithName[0]).Checked = (setting_dictionary[value.Key].defaultValue == "1");
                    }
                    else
                    {
                        controlWithName[0].Text = setting_dictionary[value.Key].defaultValue;
                    }
                }

            }
            loadDefaultDone.Visible = true; //notify user that 'defaulting the list' operation is successful.
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            Close();
        }

    }
}
