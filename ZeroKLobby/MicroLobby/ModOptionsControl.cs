using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using PlasmaShared;
using PlasmaShared.UnitSyncLib;
using ZeroKLobby;

namespace ZeroKLobby.MicroLobby
{
    class ModOptionsControl: UserControl
    {
        TextBox changesBox;
        Mod mod;
        Dictionary<string, Control> optionControls;
        Dictionary<string, Option> options;
        TabControl tabControl;

        ToolTip tooltip = new ToolTip();

        public ModOptionsControl()
        {
            changesBox = new TextBox { Multiline = true, ReadOnly = true, BackColor = Color.White, Dock = DockStyle.Fill };
            var changesTab = new TabPage { Text = "Changes" };
            changesTab.Controls.Add(changesBox);
            tabControl = new TabControl { Dock = DockStyle.Fill };
            tabControl.TabPages.Add(changesTab);
            Controls.Add(tabControl);
            tooltip.UseFading = false;
            tooltip.UseAnimation = false;
            tooltip.InitialDelay = 0;
            tooltip.ReshowDelay = 0;
            tooltip.AutoPopDelay = 10000;
            tooltip.BackColor = Color.FromArgb(255, 255, 225); // its ignored by it
        }

        protected override void Dispose(bool disposing)
        {
            EventExtensions.UnsubscribeEvents(Program.TasClient, this);
            base.Dispose(disposing);
        }


        public void HandleMod(Mod mod)
        {
            this.mod = mod;
            SuspendLayout();
            try
            {
                options = mod.Options.ToDictionary(option => option.Key.ToUpper());
                var sections = mod.Options.GroupBy(option => option.Section.ToUpper()).OrderBy(group => group.Key.ToUpper()).ToArray();
                var sectionNames = mod.Options.Where(option => option.Type == OptionType.Section).ToDictionary(option => option.Key.ToUpper(),
                                                                                                               option => option.Name);
                optionControls = new Dictionary<string, Control>();

                const int rowHeight = 30;
                foreach (var section in sections)
                {
                    if (!section.Any()) continue;

                    // set the section title
                    string sectionName;
                    if (String.IsNullOrEmpty(section.Key)) sectionName = "Other";
                    else if (!sectionNames.TryGetValue(section.Key.ToUpper(), out sectionName)) sectionName = section.Key.ToLower();
                    var tabPage = new TabPage { Text = sectionName, Name = sectionName, AutoScroll = true };
                    tabControl.TabPages.Add(tabPage);

                    var sectionY = 20;

                    foreach (var option in section)
                    {
                        Control control = null;
                        switch (option.Type)
                        {
                            case OptionType.Bool:
                                control = new CheckBox { Checked = option.Default != "0" };
                                break;
                            case OptionType.Number:
                                control = new NumericUpDown
                                          {
                                              Minimum = (decimal)option.Min,
                                              Maximum = (decimal)option.Max,
                                              Increment = (decimal)option.Step,
                                              Value = decimal.Parse(option.Default, CultureInfo.InvariantCulture),
                                              DecimalPlaces = (int)(-Math.Floor(Math.Min(0, Math.Log10(option.Step)))),
                                          };
                                break;
                            case OptionType.String:
                                control = new TextBox { Text = option.Default };
                                break;
                            case OptionType.List:
                                var optionIndex = option.ListOptions.IndexOf(option.ListOptions.Single(o => o.Key == option.Default));
                                var box = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList};
                                box.Items.AddRange(option.ListOptions.Select(o => o.Name).ToArray());
																if (optionIndex > -1) box.SelectedIndex = optionIndex;
                                control = box;
                                break;
                        }
                        if (control != null)
                        {
                            var tooltipText = string.Format("{0}\nDefault: {1} = {2}", option.Description, option.Key, option.Default);
                            tooltip.SetToolTip(control, tooltipText);
                            var label = new Label { Text = option.Name, Location = new Point(3, sectionY), Width = 145 };
                            tooltip.SetToolTip(label, tooltipText);
                            tabPage.Controls.Add(label);
                            control.Location = new Point(150, sectionY);
                            optionControls[option.Key.ToUpper()] = control;
                            tabPage.Controls.Add(control);
                            sectionY += rowHeight;
                        }

                    }
                }
            }
            catch (Exception e)
            {
                Trace.TraceError("Error in creating mod options controls: " + e);
            }
            ResumeLayout();
            SetScriptTags(Program.TasClient.MyBattle.ScriptTags);
        }

        public void SetScriptTags(IEnumerable<string> tags)
        {
            changesBox.Text = ModStore.GetModOptionSummary(mod, tags, true);
            if (changesBox.Text.Length == 0) changesBox.Text = "All options are set to their default value.";
            try
            {
                foreach (var setOption in Mod.GetModOptionPairs(tags))
                {
                    var control = optionControls[setOption.Key.ToUpper()];
                    var option = options[setOption.Key.ToUpper()];
                    switch (option.Type)
                    {
                        case OptionType.Bool:
                            var box = (CheckBox)control;
                            box.Checked = setOption.Value != "0";
                            break;
                        case OptionType.List:
                        {
                            var optionIndex = option.ListOptions.IndexOf(option.ListOptions.Single(o => o.Key.ToUpper() == setOption.Value.ToUpper()));
                            var comboBox = (ComboBox)control;
                            comboBox.SelectedIndex = optionIndex;
                        }
                            break;
                        case OptionType.Number:
                        {
                            var numeric = (NumericUpDown)control;
                            numeric.Value = decimal.Parse(setOption.Value, CultureInfo.InvariantCulture).Constrain(numeric.Minimum, numeric.Maximum);
                            break;
                        }
                        case OptionType.String:
                            control.Text = setOption.Value;
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                Trace.TraceError("Error in setting mod options: " + e);
            }
        }


        protected override void OnLoad(EventArgs ea)
        {
            base.OnLoad(ea);
            Program.TasClient.BattleDetailsChanged += (s, e) => SetScriptTags(e.ServerParams);
            Program.SpringScanner.MetaData.GetModAsync(Program.TasClient.MyBattle.ModName,
                                                       mod => { if (!Disposing && IsHandleCreated && !IsDisposed) Invoke(new Action(() => HandleMod(mod))); },
                                                       exception => { });
        }
    }
}