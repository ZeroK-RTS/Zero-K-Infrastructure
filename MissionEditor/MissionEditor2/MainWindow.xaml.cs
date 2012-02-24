using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using CMissionLib;
using CMissionLib.Actions;
using CMissionLib.Conditions;
using CMissionLib.UnitSyncLib;
using Microsoft.Win32;
using MissionEditor2.Properties;
using Action = CMissionLib.Action;
using Condition = CMissionLib.Condition;
using Trigger = CMissionLib.Trigger;

namespace MissionEditor2
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow: Window
	{
		public static readonly DependencyProperty MissionProperty;
		public TriggerLogic CurrentLogic { get { return logicGrid.SelectedItem as TriggerLogic; } }
		public Trigger CurrentTrigger { get { return logicGrid.SelectedItem as Trigger; } }
		public Region CurrentRegion { get { return logicGrid.SelectedItem as Region; } }
		public ActionsFolder CurrentActionsFolder { get { return logicGrid.SelectedItem as ActionsFolder; } }
		public ConditionsFolder CurrentConditionsFolder { get { return logicGrid.SelectedItem as ConditionsFolder; } }

		public static MainWindow Instance { get; private set; }

		public TreeView LogicGrid { get { return logicGrid; } }

		public Mission Mission { get { return (Mission)GetValue(MissionProperty); } set { SetValue(MissionProperty, value); } }
		public string SavePath { get; set; }

		static MainWindow()
		{
			MissionProperty = DependencyProperty.Register("Mission", typeof(Mission), typeof(MainWindow));
		}


		public MainWindow()
		{
			Instance = this;
			InitializeComponent();
		}

		public void QuickSave()
		{
			if (SavePath != null) Mission.SaveToXmlFile(SavePath);
			else SaveMission();
		}

		void CreateNewRepeatingTrigger()
		{
			CreateNewTrigger(true);
		}

		void CreateNewTrigger()
		{
			CreateNewTrigger(false);
		}

		void CreateNewTrigger(bool repeating)
		{
			var trigger = new Trigger { Name = GetNewTriggerName() };
			if (repeating)
			{
				trigger.MaxOccurrences = -1;
			}
			Mission.Triggers.Add(trigger);
			Mission.RaisePropertyChanged(String.Empty);
		}

		void BuildMission(bool hideFromModList = false)
		{
			var filter = "Spring Mod Archive (*.sdz)|*.sdz|All files (*.*)|*.*";
			var saveFileDialog = new SaveFileDialog { DefaultExt = "sdz", Filter = filter, RestoreDirectory = true };
			if (saveFileDialog.ShowDialog() == true)
			{
				var loadingDialog = new LoadingDialog { Owner =this };
				loadingDialog.Text = "Building Mission";
				loadingDialog.Loaded += delegate
					{
						var mission = Mission;
						var fileName = saveFileDialog.FileName;
						Utils.InvokeInNewThread(delegate
							{
								mission.CreateArchive(fileName, hideFromModList);
								var scriptPath = String.Format("{0}\\{1}.txt", Path.GetDirectoryName(fileName), Path.GetFileNameWithoutExtension(fileName));
								File.WriteAllText(scriptPath, mission.GetScript());
								this.Invoke(loadingDialog.Close);
							});
					};
				loadingDialog.ShowDialog();
			}
		}

		MenuItem GetNewActionMenu(Trigger trigger)
		{
			return GetNewActionMenu(() => trigger);
		}

		MenuItem GetNewActionMenu(Func<Trigger> getTrigger)
		{
			var menu = new MenuItem { Header = "New Action" };
			Action<string, Func<TriggerLogic>> addAction = (name, makeItem) =>
				{
					var item = new MenuItem { Header = name };
					menu.Items.Add(item);
					item.Click += (s, ea) =>
						{
							var trigger = getTrigger();
							if (trigger != null)
							{
								trigger.Logic.Add(makeItem());
								Mission.RaisePropertyChanged(String.Empty);
							}
							else
							{
								MessageBox.Show("Select the trigger that will contain the new item.");
							}
						};
				};
            String GuiMessagePersistentDesc = "Hello!\nI differ from the regular GUI message in that I can't pause the game, have no close button, and there can be only one of me!\nI require a Chili widget to work!";

            addAction("Add Objective", () => new AddObjectiveAction("newObj"));
			addAction("Allow Unit Transfers", () => new AllowUnitTransfersAction());
			addAction("Cancel Countdown", () => new CancelCountdownAction(Mission.Countdowns.FirstOrDefault()));
			addAction("Cause Defeat", () => new DefeatAction());
			addAction("Cause Sunrise", () => new SunriseAction());
			addAction("Cause Sunset", () => new SunsetAction());
			addAction("Cause Victory", () => new VictoryAction());
			addAction("Create Units", () => new CreateUnitsAction());
			addAction("Custom Action", () => new CustomAction());
			addAction("Destroy Units", () => new DestroyUnitsAction());
			addAction("Disable Triggers", () => new DisableTriggersAction());
			addAction("Display Counters", () => new DisplayCountersAction());
			addAction("Enable Triggers", () => new EnableTriggersAction());
			addAction("Execute Random Trigger", () => new ExecuteRandomTriggerAction());
			addAction("Execute Triggers", () => new ExecuteTriggersAction());
			addAction("Give Factory Orders", () => new GiveFactoryOrdersAction());
			addAction("Give Orders", () => new GiveOrdersAction());
			addAction("Lock Units", () => new LockUnitsAction());
			addAction("Make Units Always Visible", () => new MakeUnitsAlwaysVisibleAction());
			addAction("Modify Countdown", () => new ModifyCountdownAction(Mission.Countdowns.FirstOrDefault()));
			addAction("Modify Counter", () => new ModifyCounterAction());
            addAction("Modify Objective", () => new ModifyObjectiveAction("newObj"));
			addAction("Modify Score", () => new ModifyScoreAction());
			addAction("Modify Resources", () => new ModifyResourcesAction(Mission.Players.First()));
			addAction("Modify Unit Health", () => new ModifyUnitHealthAction());
			addAction("Pause", () => new PauseAction());
			addAction("Play Sound", () => new SoundAction());
			addAction("Point Camera at Map Position", () => new SetCameraPointTargetAction(Mission.Map.Texture.Width/2, Mission.Map.Texture.Height/2));
			addAction("Point Camera at Unit", () => new SetCameraUnitTargetAction());
			addAction("Send Scores", () => new SendScoreAction());
			addAction("Show Console Message", () => new ConsoleMessageAction("Hello!"));
			addAction("Show GUI Message", () => new GuiMessageAction("Hello!"));
            addAction("Show GUI Message (Persistent)", () => new GuiMessagePersistentAction(GuiMessagePersistentDesc));
            addAction("Hide GUI Message (Persistent)", () => new HideGuiMessagePersistentAction());
			addAction("Show Marker Point", () => new MarkerPointAction(Mission.Map.Texture.Width/2, Mission.Map.Texture.Height/2));
			addAction("Start Countdown", () => new StartCountdownAction(GetNewCountdownName()));
			addAction("Transfer Units", () => new TransferUnitsAction(Mission.Players.First()));
			addAction("Unlock Units", () => new UnlockUnitsAction());
			addAction("Wait", () => new WaitAction());
			return menu;
		}

		MenuItem GetNewConditionMenu(Trigger trigger)
		{
			return GetNewConditionMenu(() => trigger);
		}

		MenuItem GetNewConditionMenu(Func<Trigger> getTrigger)
		{
			var menu = new MenuItem { Header = "New Condition" };
			Action<string, Func<TriggerLogic>> addAction = (name, makeItem) =>
				{
					var item = new MenuItem { Header = name };
					menu.Items.Add(item);
					item.Click += (s, ea) =>
						{
							var trigger = getTrigger();
							if (trigger != null)
							{
								trigger.Logic.Add(makeItem());
								Mission.RaisePropertyChanged(String.Empty);
							}
							else
							{
								MessageBox.Show("Select the trigger that will contain the new item.");
							}
						};
				};

			addAction("Countdown Ended", () => new CountdownEndedCondition(Mission.Countdowns.FirstOrDefault()));
			addAction("Countdown Ticks", () => new CountdownTickCondition(Mission.Countdowns.FirstOrDefault()));
			addAction("Counter Modified", () => new CounterModifiedCondition());
			addAction("Custom Condition", () => new CustomCondition());
			addAction("Game Ends", () => new GameEndedCondition());
			addAction("Game Starts", () => new GameStartedCondition());
			addAction("Metronome Ticks", () => new TimeCondition());
			addAction("Player Died", () => new PlayerDiedCondition(Mission.Players.First()));
			addAction("Player Joined", () => new PlayerJoinedCondition(Mission.Players.First()));
			addAction("Time Elapsed", () => new TimerCondition());
			addAction("Time Left in Countdown", () => new TimeLeftInCountdownCondition(Mission.Countdowns.FirstOrDefault()));
			addAction("Unit Built On Ghost", () => new UnitBuiltOnGhostCondition());
			addAction("Unit Created", () => new UnitCreatedCondition());
			addAction("Unit Damaged", () => new UnitDamagedCondition());
			addAction("Unit Destroyed", () => new UnitDestroyedCondition());
			addAction("Unit Finished", () => new UnitFinishedCondition());
			addAction("Unit Finished In Factory", () => new UnitFinishedInFactoryCondition());
			addAction("Unit Is Visible", () => new UnitIsVisibleCondition());
			addAction("Unit Selected", () => new UnitSelectedCondition());
			addAction("Units Are In Area", () => new UnitsAreInAreaCondition());

			return menu;
		}

		string GetNewCountdownName()
		{
			for (var i = 1;; i++)
			{
				var name = string.Format("Countdown {0}", i);
				if (!Mission.Countdowns.Contains(name)) return name;
			}
		}

		string GetNewRegionName()
		{
			for (var i = 1; ; i++)
			{
				var name = string.Format("Region {0}", i);
				if (!Mission.Regions.Any(r => r.Name == name)) return name;
			}
		}
		string GetNewTriggerName()
		{
			for (var i = 1;; i++)
			{
				var name = string.Format("Trigger {0}", i);
				if (!Mission.TriggerNames.Contains(name)) return name;
			}
		}


		void MoveCurrentItem(MoveDirection direction)
		{
			if (logicGrid.SelectedItem is Trigger)
			{
				MoveTrigger(direction, CurrentTrigger);
			}
			else if (logicGrid.SelectedItem is TriggerLogic)
			{
				MoveItem(direction, CurrentLogic);
			}
			else if (logicGrid.SelectedItem == null) MessageBox.Show("No item selected.");
			else MessageBox.Show("Moving the selected item is not supported.");
		}

		void MoveItem(MoveDirection direction, TriggerLogic item)
		{
			if (item == null) return;
			var trigger = Mission.FindLogicOwner(item);
			var index = trigger.Logic.IndexOf(item);
			if (direction == MoveDirection.Up)
			{
				if (index == 0) return;
				trigger.Logic.Move(index, index - 1);
			}
			else if (direction == MoveDirection.Down)
			{
				if (index + 2 > Mission.Triggers.Count) return;
				trigger.Logic.Move(index, index + 1);
			}
			var displacedType = trigger.Logic[index];
			if ((displacedType is Action && item is Condition) ||
				(displacedType is Condition && item is Action)) MoveItem(direction, item);
			Mission.RaisePropertyChanged("AllLogic");
		}

		void MoveTrigger(MoveDirection direction, Trigger trigger)
		{
			if (trigger == null) return;
			var index = Mission.Triggers.IndexOf(trigger);
			if (direction == MoveDirection.Up)
			{
				if (index == 0) return;
				Mission.Triggers.Move(index, index - 1);
			}
			else if (direction == MoveDirection.Down)
			{
				if (index + 2 > Mission.Triggers.Count) return;
				Mission.Triggers.Move(index, index + 1);
			}
			Mission.RaisePropertyChanged("AllLogic");
		}

		void RenameLogicItem(TriggerLogic item)
		{
			if (item == null) return;
			var dialog = new StringRequest { Title = "Rename Item", TextBox = { Text = item.Name }, Owner =this };
			if (dialog.ShowDialog() == true) item.Name = dialog.TextBox.Text;
		}

		void RenameRegion(Region region)
		{
			if (region == null) return;
			var dialog = new StringRequest { Title = "Rename Region", TextBox = { Text = region.Name }, Owner = this };
			if (dialog.ShowDialog() == true)
			{
				region.Name = dialog.TextBox.Text;
				region.RaisePropertyChanged(String.Empty);
				Mission.RaisePropertyChanged("Regions");
			}
		}

		void RenameCurrentItem()
		{
			if (logicGrid.SelectedItem is Trigger)
			{
				RenameTrigger(CurrentTrigger);
			}
			else if (logicGrid.SelectedItem is TriggerLogic)
			{
				RenameLogicItem(CurrentLogic);
			}
			else if (logicGrid.SelectedItem is Region)
			{
				RenameRegion(CurrentRegion);
			}
			else if (logicGrid.SelectedItem == null) MessageBox.Show("No item selected.");
			else MessageBox.Show("Renaming the selected item is not supported.");
		}

		void DeleteCurrentItem()
		{
			if (logicGrid.SelectedItem is Trigger)
			{
				DeleteTrigger(CurrentTrigger);
			}
			else if (logicGrid.SelectedItem is TriggerLogic)
			{
				DeleteTriggerLogic(CurrentLogic);
			}
			else if (logicGrid.SelectedItem is Region)
			{
				DeleteRegion(CurrentRegion);
			}
			else if (logicGrid.SelectedItem == null) MessageBox.Show("No item selected.");
			else MessageBox.Show("Deleting the selected item is not supported.");
		}

		void RenameTrigger(Trigger trigger)
		{
			if (trigger == null) return;
			var dialog = new StringRequest { Title = "Rename Trigger", TextBox = { Text = trigger.Name }, Owner =this };
			if (dialog.ShowDialog() == true)
			{
				trigger.Name = dialog.TextBox.Text;
				trigger.RaisePropertyChanged(String.Empty);
				Mission.RaisePropertyChanged("Triggers");
			}
		}

		void SaveMission()
		{
			var saveFileDialog = new SaveFileDialog
			                     { DefaultExt = WelcomeDialog.MissionExtension, Filter = WelcomeDialog.MissionDialogFilter, RestoreDirectory = true };
			if (saveFileDialog.ShowDialog() == true)
			{
				SavePath = saveFileDialog.FileName;
				Settings.Default.MissionPath = saveFileDialog.FileName;
				Settings.Default.Save();
				Mission.SaveToXmlFile(saveFileDialog.FileName);
			}
		}

		void ShowMissionManagement()
		{
			new MissionManagement { Owner = this }.ShowDialog();
		}

		void ShowMissionSettings()
		{
			new MissionSettingsDialog { Owner = this }.ShowDialog();
		}

		/// <summary>
		/// create testmission.sdz, the script.txt, run spring, capture output
		/// </summary>
		void TestMission()
		{
			var springPath = Settings.Default.SpringPath;
			var unitSync = new UnitSync(Settings.Default.SpringPath);
			var springExe = springPath + "\\spring.exe";
			var realName = Mission.Name;
			string scriptFile = null;
			try
			{
				var missionFile = "testmission.sdz";
                var writeablePath = unitSync.WritableDataDirectory;
				var missionPath = writeablePath + "\\games\\" + missionFile;
				scriptFile = writeablePath + "\\script.txt";
				Mission.Name = Mission.Name + " Test";
				File.WriteAllText(scriptFile, Mission.GetScript());
				Mission.CreateArchive(missionPath, true);
			}
			catch(Exception e)
			{
				if (Debugger.IsAttached) throw;
				MessageBox.Show(e.Message);
			}
			finally
			{
				unitSync.Dispose();
				Mission.Name = realName;
			}
			var startInfo = new ProcessStartInfo
			                {
			                	FileName = springExe,
			                	Arguments = String.Format("\"{0}\"", scriptFile),
			                	RedirectStandardError = true,
			                	RedirectStandardOutput = true,
			                	UseShellExecute = false
			                };
			var springProcess = new Process { StartInfo = startInfo };
			Utils.InvokeInNewThread(delegate
				{
					if (!springProcess.Start()) throw new Exception("Failed to start Spring");
					while (!springProcess.HasExited)
					{
						var line = springProcess.StandardOutput.ReadLine();
						if (!String.IsNullOrEmpty(line)) Console.WriteLine(line);
						var output = springProcess.StandardOutput.ReadToEnd();
						if (!String.IsNullOrEmpty(output)) Console.WriteLine(output);
					}
				});
		}

		void DeleteTriggerLogic(TriggerLogic item)
		{
			var trigger = Mission.FindLogicOwner(item);
			trigger.Logic.Remove(item);
			Mission.RaisePropertyChanged(String.Empty);
		}

		void logic_Loaded(object sender, RoutedEventArgs e)
		{
			var border = (Border)e.Source;
			var item = (TriggerLogic)border.DataContext;
			var menu = new ContextMenu();
			border.ContextMenu = menu;
			menu.AddAction("Rename", () => RenameLogicItem(item));
			menu.AddAction("Move Up", () => MoveItem(MoveDirection.Up, item));
			menu.AddAction("Move Down", () => MoveItem(MoveDirection.Down, item));
			menu.AddAction("Delete", () => DeleteTriggerLogic(item));
			border.ContextMenu = menu;
		}

		void GuiMessageButtonLoaded(object sender, RoutedEventArgs e)
		{
			var button = (Button)e.Source;
			button.Click += delegate
				{
					var filter = "Image Files(*.BMP;*.JPG;*.PNG)|*.BMP;*.JPG;*.PNG|All files (*.*)|*.*";
					var dialog = new OpenFileDialog { Filter = filter, RestoreDirectory = true };
					if (dialog.ShowDialog() == true)
					{
						var action = (GuiMessageAction)button.Tag;
						action.ImagePath = dialog.FileName;
					}
				};
		}

        void GuiMessagePersistentButtonLoaded(object sender, RoutedEventArgs e)
        {
            var button = (Button)e.Source;
            button.Click += delegate
            {
                var filter = "Image Files(*.BMP;*.JPG;*.PNG)|*.BMP;*.JPG;*.PNG|All files (*.*)|*.*";
                var dialog = new OpenFileDialog { Filter = filter, RestoreDirectory = true };
                if (dialog.ShowDialog() == true)
                {
                    var action = (GuiMessagePersistentAction)button.Tag;
                    action.ImagePath = dialog.FileName;
                }
            };
        }

		void UnitDestroyedGroupsListLoaded(object sender, RoutedEventArgs e)
		{
			var collection = ((UnitDestroyedCondition)CurrentLogic).Groups;
			((ListBox)e.Source).BindCollection(collection);
		}

		void action_Loaded(object sender, RoutedEventArgs e)
		{
			logic_Loaded(sender, e);
		}

		void condition_Loaded(object sender, RoutedEventArgs e)
		{
			logic_Loaded(sender, e);
		}

		void CreateNewRegion()
		{
			var region = new Region { Name = GetNewRegionName() };
			Mission.Regions.Add(region);
		}

		void DeleteTrigger(Trigger trigger)
		{
			Mission.Triggers.Remove(trigger);
			Mission.RaisePropertyChanged(String.Empty);
		}

		void trigger_Loaded(object sender, RoutedEventArgs e)
		{
			var border = (Border)e.Source;
			var trigger = (Trigger)border.DataContext;
			var menu = new ContextMenu();
			menu.AddAction("New Trigger", CreateNewTrigger);
			menu.AddAction("New Trigger (Repeating)", CreateNewRepeatingTrigger);
			menu.Items.Add(GetNewActionMenu(trigger));
			menu.Items.Add(GetNewConditionMenu(trigger));
			menu.Items.Add(new Separator());
			menu.AddAction("Move Up", () => MoveTrigger(MoveDirection.Up, trigger));
			menu.AddAction("Move Down", () => MoveTrigger(MoveDirection.Down, trigger));
			menu.AddAction("Rename", () => RenameTrigger(trigger));
			menu.AddAction("Delete", () => DeleteTrigger(trigger));
			menu.Items.Add(new Separator());
			menu.AddAction("Expand All Triggers", ExpandAllTriggers);
			menu.AddAction("Collapse All Triggers", CollapseAllTriggers);
			menu.AddAction("Collapse All But This", () => CollapseAllButThisTrigger(trigger));
			border.ContextMenu = menu;
		}




		void CollapseAllButThisTrigger(Trigger thisTrigger)
		{
			foreach (var trigger in Mission.Triggers)
			{
				trigger.IsExpanded = thisTrigger == trigger;
			}
		}

		void ExpandAllTriggers()
		{
			foreach (var trigger in Mission.Triggers)
			{
				trigger.IsExpanded = true;
			}
		}

		void CollapseAllTriggers()
		{
			foreach (var trigger in Mission.Triggers)
			{
				trigger.IsExpanded = false;
			}
		}

		void window_Loaded(object sender, RoutedEventArgs e)
		{
			var project = MainMenu.AddContainer("Project");
			project.AddAction("New", WelcomeDialog.PromptForNewMission);
			project.AddAction("Open", WelcomeDialog.AskForExistingMission);
			project.AddAction("Save", QuickSave);
			project.AddAction("Save As", SaveMission);
			var mission = MainMenu.AddContainer("Mission");
			mission.AddAction("Create Mutator", () => BuildMission());
			mission.AddAction("Create Invisible Mutator", () => BuildMission(true));
			mission.AddAction("Test Mission", TestMission);
			mission.AddAction("Publish", ShowMissionManagement);
			mission.AddAction("Settings", ShowMissionSettings);


			var editMenu = MainMenu.AddContainer("Edit");
			editMenu.AddAction("Rename", RenameCurrentItem);
			editMenu.AddAction("Delete", DeleteCurrentItem);
			editMenu.AddAction("Move Up", () => MoveCurrentItem(MoveDirection.Up));
			editMenu.AddAction("Move Down", () => MoveCurrentItem(MoveDirection.Down));
			editMenu.AddAction("Expand All Triggers", ExpandAllTriggers);
			editMenu.AddAction("Collapse All Triggers", CollapseAllTriggers);

			//var help = MainMenu.AddContainer("Help");
			//help.AddAction("Basic Help", () => new Help().ShowDialog());

			var newMenu = MainMenu.AddContainer("New");
			newMenu.AddAction("New Trigger", CreateNewTrigger);
			newMenu.AddAction("New Trigger (Repeating)", CreateNewRepeatingTrigger);
			newMenu.AddAction("New Region", CreateNewRegion);
			newMenu.Items.Add(GetNewConditionMenu(delegate
			{
				if (logicGrid.SelectedItem is Trigger) return CurrentTrigger;
				if (logicGrid.SelectedItem is ActionsFolder) return CurrentActionsFolder.Trigger;
				if (logicGrid.SelectedItem is ConditionsFolder) return CurrentConditionsFolder.Trigger;
				if (logicGrid.SelectedItem is TriggerLogic) return Mission.FindLogicOwner(CurrentLogic);
				return null;
			}));
			newMenu.Items.Add(GetNewActionMenu(() => CurrentTrigger));

			var welcomeScreen = new WelcomeDialog { ShowInTaskbar = true, Owner = this };
			welcomeScreen.ShowDialog();
			if (Mission == null)
			{
				MessageBox.Show("A mission needs to be selected");
				Environment.Exit(0);
			}



			var menu = new ContextMenu();
			menu.AddAction("New Trigger", CreateNewTrigger);
			menu.AddAction("New Trigger (Repeating)", CreateNewRepeatingTrigger);
			menu.AddAction("New Region", CreateNewRegion);
			logicGrid.ContextMenu = menu;
		}

		enum MoveDirection
		{
			Up,
			Down
		}

		private void conditionsFolder_Loaded(object sender, RoutedEventArgs e)
		{
			var border = (Border) e.Source;
			var folder = (ConditionsFolder)border.DataContext;
			var menu = new ContextMenu();
			menu.Items.Add(GetNewConditionMenu(folder.Trigger));
			border.ContextMenu = menu;

		}

		private void actionsFolder_Loaded(object sender, RoutedEventArgs e)
		{
			var border = (Border)e.Source;
			var folder = (ActionsFolder)border.DataContext;
			var menu = new ContextMenu();
			menu.Items.Add(GetNewActionMenu(folder.Trigger));
			border.ContextMenu = menu;
		}

		private void region_Loaded(object sender, RoutedEventArgs e)
		{
			var border = (Border)e.Source;
			var region = (Region)border.DataContext;
			var menu = new ContextMenu();
			menu.AddAction("New Region", CreateNewRegion);
			menu.AddAction("Rename", () => RenameRegion(region));
			menu.AddAction("Delete", () => DeleteRegion(region));
			border.ContextMenu = menu;

		}

		void DeleteRegion(Region region)
		{
			Mission.Regions.Remove(region);
		}
	}
}