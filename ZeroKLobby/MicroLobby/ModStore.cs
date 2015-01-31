using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using PlasmaDownloader;
using ZkData;
using ZkData.UnitSyncLib;

namespace ZeroKLobby.MicroLobby
{
	/// <summary>
	/// pre-computes mod data that might be required in the gui
	/// </summary>
	class ModStore
	{
		readonly object locker = new object();
		Mod mod;
		public Ai[] Ais;

		public string ChangedOptions { get; private set; }

		public ModStore()
		{
			Program.TasClient.BattleDetailsChanged += (s, e) => SetScriptTags(e.ServerParams);
			Program.TasClient.BattleJoined += (s, e) =>
				{
					if (mod != null && mod.Name != Program.TasClient.MyBattle.ModName)
					{
						Ais = null;
						mod = null;
					}
					Program.SpringScanner.MetaData.GetModAsync(Program.TasClient.MyBattle.ModName, HandleMod, exception => { });
				};
		}

		public static string GetModOptionSummary(Mod mod, IEnumerable<string> tags, bool useSectionHeaders)
		{
			try
			{
				var setOptions = Mod.GetModOptionPairs(tags);

				var sectionNames = mod.Options.Where(o => o.Type == OptionType.Section).ToDictionary(o => o.Key.ToUpper(), o => o.Name);

				var sections = from option in mod.Options
				               join pair in setOptions on option.Key equals pair.Key
				               where pair.Value != option.Default
				               group new { pair, option } by option.Section;

				var optionBuilder = new StringBuilder();
				foreach (var section in sections)
				{
					var indent = false;
					if (useSectionHeaders && !String.IsNullOrEmpty(section.Key))
					{
						indent = true;
						optionBuilder.AppendLine(sectionNames[section.Key.ToUpper()]);
						optionBuilder.AppendLine();
					}
					foreach (var o in section)
					{
						string line = null;
						switch (o.option.Type)
						{
							case OptionType.Bool:
								line = o.option.Name + ": " + (o.pair.Value == "0" ? "No" : "Yes");
								break;
							case OptionType.Number:
								line = o.option.Name + ": " + o.pair.Value;
								break;
							case OptionType.String:
								line = o.option.Name + ": " + o.pair.Value;
								break;
							case OptionType.List:
								var valueName = o.option.ListOptions.Single(opt => opt.Key == o.pair.Value).Name;
								line = o.option.Name + ": " + valueName;
								break;
						}
						optionBuilder.AppendLine((indent ? "\t" : String.Empty) + line);
					}
					if (useSectionHeaders) optionBuilder.AppendLine();
				}

				return optionBuilder.ToString();
			}
			catch (Exception e)
			{
				Trace.WriteLine("Error in building mod option summary: " + e);
				return String.Empty;
			}
		}

		public event EventHandler<EventArgs<Mod>> ModLoaded = delegate { };

		public void HandleMod(Mod mod)
		{
			if (mod != null)
			{
			  Ais = mod.ModAis;
				ModLoaded(this, new EventArgs<Mod>(mod));
				lock (locker)
				{
					if (Program.TasClient.MyBattle.ModName == mod.Name)
					{
						this.mod = mod;
						ChangedOptions = GetModOptionSummary(mod, Program.TasClient.MyBattle.ScriptTags, false);

					}
					else return;
				}
			}
		}

		public void SetScriptTags(IEnumerable<string> tags)
		{
			lock (locker) if (mod != null) ChangedOptions = GetModOptionSummary(mod, tags, false);
		}
	}
}