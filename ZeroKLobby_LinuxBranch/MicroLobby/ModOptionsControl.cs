using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using LobbyClient;
using PlasmaShared;
using PlasmaShared.UnitSyncLib;

namespace ZeroKLobby.MicroLobby
{
  class ModOptionsControl: UserControl
  {
    readonly TextBox changesBox;
    Mod mod;
    Dictionary<string, Control> optionControls;
    Dictionary<string, Option> options;
    TabControl tabControl;
    private Button butApplyChanges;
    private Label proposedLabel;

    readonly ToolTip tooltip = new ToolTip();

    public ModOptionsControl()
    {
      InitializeComponent();
      changesBox = new TextBox { Multiline = true, ReadOnly = true, BackColor = Color.White, Dock = DockStyle.Fill };
      var changesTab = new TabPage { Text = "Changes" };
      changesTab.Controls.Add(changesBox);
      tabControl.TabPages.Add(changesTab);
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

    public IEnumerable<KeyValuePair<string, string>> GetProposedChanges()
    {
      var setVals = new Dictionary<string, string>();
      foreach (var oc in optionControls)
      {
        var opt = options[oc.Key];
        if (oc.Value is CheckBox) setVals[oc.Key] = ((CheckBox)oc.Value).Checked ? "1" : "0";
        else if (oc.Value is NumericUpDown) setVals[oc.Key] = ((double)((NumericUpDown)oc.Value).Value).ToString("G",CultureInfo.InvariantCulture);
        else if (oc.Value is ComboBox) setVals[oc.Key] = opt.ListOptions[((ComboBox)oc.Value).SelectedIndex].Key;
        else setVals[oc.Key] = ((TextBox)oc.Value).Text;
      }

      var mb = Program.TasClient.MyBattle;
      var changes =
        setVals.Where(
          x =>
          (!mb.ModOptions.ContainsKey(x.Key) && x.Value != mod.Options.First(y => y.Key == x.Key).Default) ||
          (mb.ModOptions.ContainsKey(x.Key) && x.Value != mb.ModOptions[x.Key]));

      if (changes.Any()) proposedLabel.Text = string.Format(" Change: {0}",
                                           string.Join(", ", changes.Select(x => string.Format("{0}={1}", options[x.Key].Name, x.Value)).ToArray()));
      else proposedLabel.Text = "";

      return changes;
    }


    public void HandleMod(Mod mod)
    {
      this.mod = mod;
      SuspendLayout();
      try
      {
        options = mod.Options.ToDictionary(option => option.Key);
        var sections = mod.Options.GroupBy(option => option.Section).OrderBy(group => group.Key).ToArray();
        var sectionNames = mod.Options.Where(option => option.Type == OptionType.Section).ToDictionary(option => option.Key, option => option.Name);
        optionControls = new Dictionary<string, Control>();

        const int rowHeight = 30;
        foreach (var section in sections)
        {
          if (!section.Any()) continue;

          // set the section title
          string sectionName;
          if (String.IsNullOrEmpty(section.Key)) sectionName = "Other";
          else if (!sectionNames.TryGetValue(section.Key, out sectionName)) sectionName = section.Key.ToLower();
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
                ((CheckBox)control).CheckedChanged += proposedChanged;
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
                ((NumericUpDown)control).ValueChanged += proposedChanged;
                break;
              case OptionType.String:
                control = new TextBox { Text = option.Default };
                control.TextChanged += proposedChanged;
                break;
              case OptionType.List:
                var optionIndex = option.ListOptions.IndexOf(option.ListOptions.Single(o => o.Key == option.Default));
                var box = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
                box.Items.AddRange(option.ListOptions.Select(o => o.Name).ToArray());
                if (optionIndex > -1) box.SelectedIndex = optionIndex;
                control = box;
                box.SelectedIndexChanged += proposedChanged;
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
              optionControls[option.Key] = control;
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
          var control = optionControls[setOption.Key];
          var option = options[setOption.Key];
          switch (option.Type)
          {
            case OptionType.Bool:
              var box = (CheckBox)control;
              box.Checked = setOption.Value != "0";
              break;
            case OptionType.List:
            {
              var optionIndex = option.ListOptions.IndexOf(option.ListOptions.Single(o => o.Key == setOption.Value));
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
                                                 exception => { }, Program.SpringPaths.SpringVersion);
    }


    void proposedChanged(object sender, EventArgs e)
    {
      GetProposedChanges();
    }

    private void InitializeComponent()
    {
      this.tabControl = new System.Windows.Forms.TabControl();
      this.butApplyChanges = new System.Windows.Forms.Button();
      this.proposedLabel = new System.Windows.Forms.Label();
      this.SuspendLayout();
      // 
      // tabControl
      // 
      this.tabControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                  | System.Windows.Forms.AnchorStyles.Left)
                  | System.Windows.Forms.AnchorStyles.Right)));
      this.tabControl.Location = new System.Drawing.Point(0, 3);
      this.tabControl.Name = "tabControl";
      this.tabControl.SelectedIndex = 0;
      this.tabControl.Size = new System.Drawing.Size(687, 222);
      this.tabControl.TabIndex = 0;
      // 
      // butApplyChanges
      // 
      this.butApplyChanges.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
      this.butApplyChanges.Location = new System.Drawing.Point(3, 231);
      this.butApplyChanges.Name = "butApplyChanges";
      this.butApplyChanges.Size = new System.Drawing.Size(89, 23);
      this.butApplyChanges.TabIndex = 1;
      this.butApplyChanges.Text = "Apply changes";
      this.butApplyChanges.UseVisualStyleBackColor = true;
      this.butApplyChanges.Click += new System.EventHandler(this.applyChanges_Click);
      // 
      // proposedLabel
      // 
      this.proposedLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                  | System.Windows.Forms.AnchorStyles.Right)));
      this.proposedLabel.AutoSize = true;
      this.proposedLabel.Location = new System.Drawing.Point(98, 236);
      this.proposedLabel.Name = "proposedLabel";
      this.proposedLabel.Size = new System.Drawing.Size(59, 13);
      this.proposedLabel.TabIndex = 2;
      this.proposedLabel.Text = "Change list";
      // 
      // ModOptionsControl
      // 
      this.Controls.Add(this.proposedLabel);
      this.Controls.Add(this.butApplyChanges);
      this.Controls.Add(this.tabControl);
      this.Name = "ModOptionsControl";
      this.Size = new System.Drawing.Size(687, 257);
      this.ResumeLayout(false);
      this.PerformLayout();

    }

    private void applyChanges_Click(object sender, EventArgs e)
    {
      var vals = GetProposedChanges();
      Program.TasClient.Say(TasClient.SayPlace.Battle, "", "!setoptions " + string.Join(",", vals.Select(x => string.Format("{0}={1}", x.Key, x.Value))), false);
    }
  }
}