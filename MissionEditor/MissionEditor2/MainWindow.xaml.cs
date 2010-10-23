using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using CMissionLib;
using CMissionLib.Actions;
using CMissionLib.Conditions;
using CMissionLib.UnitSyncLib;
using Microsoft.Win32;
using MissionEditor2.Properties;
using Action = System.Action;
using Path = System.IO.Path;
using Trigger = CMissionLib.Trigger;

namespace MissionEditor2
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public static readonly DependencyProperty MissionProperty;
		MenuItem regionsMenu;

		static MainWindow()
		{

			MissionProperty = DependencyProperty.Register("Mission", typeof (Mission), typeof (MainWindow));
		}


		public MainWindow()
		{
			Instance = this;
			InitializeComponent();
		}

		public static MainWindow Instance { get; private set; }

		public ListBox LogicGrid
		{
			get { return logicGrid; }
		}

		public Mission Mission
		{
			get { return (Mission) GetValue(MissionProperty); }
			set { SetValue(MissionProperty, value); }
		}

		public Trigger CurrentTrigger
		{
			get { return Mission.FindLogicOwner(CurrentLogic); }
		}

		public TriggerLogic CurrentLogic
		{
			get { return (TriggerLogic) logicGrid.SelectedItem; }
		}

		public void LogicButton_Loaded(object sender, RoutedEventArgs e)
		{
			var button = (DropDownButton) e.Source;
			if (button.Tag is Trigger)
			{
				button.PreviewMouseUp += (s, ea) => AddNewTrigger();
			}
			else if (button.Tag is KeyValuePair<string, Trigger>)
			{
				var kvp = (KeyValuePair<string, Trigger>) button.Tag;
				var trigger = kvp.Value;
				var dropDown = new ContextMenu();
				button.DropDown = dropDown;
				Action<string, Func<TriggerLogic>> addAction = (name, makeItem) =>
					{
						var item = new MenuItem {Header = name};
						dropDown.Items.Add(item);
						item.Click += (s, ea) =>
							{
								trigger.Logic.Add(makeItem());
								Mission.RaisePropertyChanged(String.Empty);
							};
					};
				if (kvp.Key == "Conditions")
				{
					addAction("Countdown Ended", () => new CountdownEndedCondition(Mission.Countdowns.FirstOrDefault()));
					addAction("Countdown Ticks", () => new CountdownTickCondition(Mission.Countdowns.FirstOrDefault()));
					addAction("Counter Modified", () => new CounterModifiedCondition());
					addAction("Custom Condition", () => new CustomCondition());
					addAction("Game Ends", () => new GameEndedCondition());
					addAction("Game Starts", () => new GameStartedCondition());
					addAction("Metronome Clicks", () => new TimeCondition());
					addAction("Player Died", () => new PlayerDiedCondition(Mission.Players.First()));
					addAction("Player Joined", () => new PlayerJoinedCondition(Mission.Players.First()));
					addAction("Time Left in Countdown", () => new TimeLeftInCountdownCondition(Mission.Countdowns.FirstOrDefault()));
					addAction("Unit Created", () => new UnitCreatedCondition());
					addAction("Unit Damaged", () => new UnitDamagedCondition());
					addAction("Unit Destroyed", () => new UnitDestroyedCondition());
					addAction("Unit Finished", () => new UnitFinishedCondition());
					addAction("Unit Finished In Factory", () => new UnitFinishedInFactoryCondition());
					addAction("Unit Is Visible", () => new UnitIsVisibleCondition());
					addAction("Unit Selected", () => new UnitSelectedCondition());
					addAction("Units Are In Area", () => new UnitsAreInAreaCondition());
				}
				else if (kvp.Key == "Actions")
				{
					var centerMapX = Mission.Map.Texture.Width/2;
					var centerMapY = Mission.Map.Texture.Height/2;
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
					addAction("Modify Score", () => new ModifyScoreAction());
					addAction("Modify Resources", () => new ModifyResourcesAction(Mission.Players.First()));
					addAction("Modify Unit Health", () => new ModifyUnitHealthAction());
					addAction("Pause", () => new PauseAction());
					addAction("Play Sound", () => new SoundAction());
					addAction("Point Camera at Map Position", () => new SetCameraPointTargetAction(centerMapX, centerMapY));
					addAction("Point Camera at Unit", () => new SetCameraUnitTargetAction());
					addAction("Send Score", () => new SendScoreAction());
					addAction("Show Console Message", () => new ConsoleMessageAction("Hello!"));
					addAction("Show GUI Message", () => new GuiMessageAction("Hello!"));
					addAction("Show Marker Point", () => new MarkerPointAction(centerMapX, centerMapY));
					addAction("Start Countdown", () => new StartCountdownAction(GetNewCountdownName()));
					addAction("Transfer Units", () => new TransferUnitsAction(Mission.Players.First()));
					addAction("Unlock Units", () => new UnlockUnitsAction());
					addAction("Wait", () => new WaitAction());
				}
				else throw new Exception("Button not recognized");
			}
			else throw new Exception("Button not recognized");
		}

		void SoundButtonLoaded(object sender, RoutedEventArgs e)
		{
			var button = (Button) e.Source;
			button.Click += delegate
				{
					var filter = "Wave Files(*.WAV)|*.WAV";
					var dialog = new OpenFileDialog {Filter = filter, RestoreDirectory = true};
					if (dialog.ShowDialog() == true)
					{
						var action = (SoundAction) LogicGrid.SelectedItem;
						action.SoundPath = dialog.FileName;
					}
				};
		}

		void GuiMessageButtonLoaded(object sender, RoutedEventArgs e)
		{
			var button = (Button) e.Source;
			button.Click += delegate
				{
					var filter = "Image Files(*.BMP;*.JPG;*.PNG)|*.BMP;*.JPG;*.PNG|All files (*.*)|*.*";
					var dialog = new OpenFileDialog {Filter = filter, RestoreDirectory = true};
					if (dialog.ShowDialog() == true)
					{
						var action = (GuiMessageAction) button.Tag;
						action.ImagePath = dialog.FileName;
					}
				};
		}

		void DeleteCurrentItem()
		{
			var selectedItem = CurrentLogic;
			var trigger = CurrentTrigger;
			trigger.Logic.Remove(selectedItem);
			Mission.RaisePropertyChanged(String.Empty);
		}

		string GetNewTriggerName()
		{
			for (var i = 1;; i++)
			{
				var name = string.Format("Trigger {0}", i);
				if (!Mission.TriggerNames.Contains(name)) return name;
			}
		}

		string GetNewCountdownName()
		{
			for (var i = 1;; i++)
			{
				var name = string.Format("Countdown {0}", i);
				if (!Mission.Countdowns.Contains(name)) return name;
			}
		}

		void AddNewTrigger()
		{
			var trigger = new Trigger {Name = GetNewTriggerName()};
			Mission.Triggers.Add(trigger);
			Mission.RaisePropertyChanged(String.Empty);
		}

		void RemoveEmptyTriggers()
		{
			foreach (var trigger in Mission.Triggers.ToArray())
			{
				if (trigger.Logic.All(l => l.Name == "Dummy"))
				{
					Mission.Triggers.Remove(trigger);
				}
			}
			Mission.RaisePropertyChanged(String.Empty);
		}

		void DeleteParentTrigger()
		{
			Mission.Triggers.Remove(CurrentTrigger);
			Mission.RaisePropertyChanged(String.Empty);
		}

		void ShowMissionManagement()
		{
			new MissionManagement().ShowDialog();
		}

		void MoveItem(MoveDirection direction, TriggerLogic item)
		{
			var trigger = Mission.FindLogicOwner(item);
			var index = trigger.Logic.IndexOf(item) + (direction == MoveDirection.Up ? -1 : 1);
			if (index >= 2 && index < trigger.Logic.Count)
			{
				trigger.Logic.Remove(item);
				trigger.Logic.Insert(index, item);
				Mission.RaisePropertyChanged("AllLogic");
				LogicGrid.SelectedIndex = new ObservableCollection<TriggerLogic>(Mission.AllLogic).IndexOf(item);
			}
		}

		void MoveTrigger(MoveDirection direction, Trigger trigger)
		{
			var index = Mission.Triggers.IndexOf(trigger) + (direction == MoveDirection.Up ? -1 : 1);
			if (index >= 2 && index < Mission.Triggers.Count)
			{
				Mission.Triggers.Remove(trigger);
				Mission.Triggers.Insert(index, trigger);
				Mission.RaisePropertyChanged("AllLogic");
				LogicGrid.SelectedIndex = new ObservableCollection<TriggerLogic>(Mission.AllLogic).IndexOf(CurrentLogic);
			}
		}

		void Renametrigger(Trigger trigger)
		{
			var dialog = new StringRequest {Title = "Rename Trigger", TextBox = {Text = trigger.Name}};
			if (dialog.ShowDialog() == true)
			{
				trigger.Name = dialog.TextBox.Text;
				trigger.RaisePropertyChanged(String.Empty);
				Mission.RaisePropertyChanged("Triggers");
			}
		}

		void RenameLogicItem(TriggerLogic item)
		{
			var dialog = new StringRequest {Title = "Rename Item", TextBox = {Text = item.Name}};
			if (dialog.ShowDialog() == true)
			{
				item.Name = dialog.TextBox.Text;
			}
		}

		void ShowTriggerSettings(Trigger trigger)
		{
			var settings = new TriggerSettings {DataContext = trigger};
			settings.ShowDialog();
		}

		void UnitDestroyedGroupsListLoaded(object sender, RoutedEventArgs e)
		{
			var collection = ((UnitDestroyedCondition) CurrentLogic).Groups;
			((ListBox) e.Source).BindCollection(collection);
		}

		void TriggerBarLoaded(object sender, RoutedEventArgs e)
		{
			var border = (Border) e.Source;
			var trigger = (Trigger) ((CollectionViewGroup) border.DataContext).Name;
			var menu = new ContextMenu();
			menu.AddAction("Move Up", () => MoveTrigger(MoveDirection.Up, trigger));
			menu.AddAction("Move Down", () => MoveTrigger(MoveDirection.Down, trigger));
			menu.AddAction("Rename", () => Renametrigger(trigger));
			menu.AddAction("Delete", delegate
				{
					Mission.Triggers.Remove(trigger);
					Mission.RaisePropertyChanged(String.Empty);
				});
			menu.AddAction("Settings", () => ShowTriggerSettings(trigger));
			border.ContextMenu = menu;
		}

		void LogicItemBarLoaded(object sender, RoutedEventArgs e)
		{
			var border = (Border) e.Source;
			var logicItem = (TriggerLogic) border.DataContext;
			var trigger = Mission.FindLogicOwner(logicItem);
			var menu = new ContextMenu();
			border.ContextMenu = menu;
			menu.AddAction("Rename", () => RenameLogicItem(logicItem));
			menu.AddAction("Move Up", () => MoveItem(MoveDirection.Up, logicItem));
			menu.AddAction("Move Down", () => MoveItem(MoveDirection.Down, logicItem));
			menu.AddAction("Delete", delegate
				{
					trigger.Logic.Remove(logicItem);
					Mission.RaisePropertyChanged(String.Empty);
				});
		}


		void window_Loaded(object sender, RoutedEventArgs e)
		{
			var project = MainMenu.AddContainer("Project");
			project.AddAction("New", WelcomeDialog.PromptForNewMission);
			project.AddAction("Open", WelcomeDialog.AskForExistingMission);
			project.AddAction("Save", QuickSave);
			project.AddAction("Save As", SaveMission);
			var mission = MainMenu.AddContainer("Mission");
			mission.AddAction("Create Mutator", BuildMission);
			mission.AddAction("Test Mission", TestMission);
			mission.AddAction("Publish", () => Utils.Publish(Mission, null));
			mission.AddAction("Manage Missions", ShowMissionManagement);
			mission.AddAction("Settings", ShowMissionSettings);
			var trigger = MainMenu.AddContainer("Trigger");
			trigger.AddAction("New", AddNewTrigger);
			trigger.AddAction("Move Up", () => MoveTrigger(MoveDirection.Up, CurrentTrigger));
			trigger.AddAction("Move Down", () => MoveTrigger(MoveDirection.Down, CurrentTrigger));
			trigger.AddAction("Rename", () => Renametrigger(CurrentTrigger));
			trigger.AddAction("Delete", DeleteParentTrigger);
			trigger.AddAction("Delete All Empty", RemoveEmptyTriggers);
			trigger.AddAction("Settings", () => ShowTriggerSettings(CurrentTrigger));
			var logic = MainMenu.AddContainer("Logic");
			logic.AddAction("Delete Item", DeleteCurrentItem);
			logic.AddAction("Rename Item", () => RenameLogicItem(CurrentLogic));
			logic.AddAction("Move Up", () => MoveItem(MoveDirection.Up, CurrentLogic));
			logic.AddAction("Move Down", () => MoveItem(MoveDirection.Down, CurrentLogic));
			// regionsMenu = MainMenu.AddContainer("Regions");
			// regionsMenu.Click += new RoutedEventHandler(regionsMenu_GotFocus);

			var help = MainMenu.AddContainer("Help");
			help.AddAction("Basic Help", () => new Help().ShowDialog());

			var welcomeScreen = new WelcomeDialog {ShowInTaskbar = true};
			welcomeScreen.ShowDialog();
			if (Mission == null)
			{
				MessageBox.Show("A mission needs to be selected");
				Environment.Exit(0);
			}


		}

		void regionsMenu_GotFocus(object sender, RoutedEventArgs e)
		{
			regionsMenu.Items.Clear();
			foreach (var region in Mission.Regions)
			{
				regionsMenu.AddAction(region.Name, delegate
				{
					var window = new RegionWindow(region);
					window.ShowDialog();
				});
			}
			var newRegionItem = new MenuItem { Header = "Create Region" };
			newRegionItem.Click += newRegionItem_Click;
			regionsMenu.Items.Add(newRegionItem);
		}

		void newRegionItem_Click(object sender, RoutedEventArgs e)
		{

			var region = new Region {Name = "New Region"};
			Mission.Regions.Add(region);
			var window = new RegionWindow(region);
			window.ShowDialog();
		}

		public string SavePath { get; set; }

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
				var missionPath = writeablePath + "\\mods\\" + missionFile;
				scriptFile = writeablePath + "\\script.txt";
				Mission.Name = Mission.Name + " Test";
				File.WriteAllText(scriptFile, Mission.GetScript());
				Mission.CreateArchive(missionPath);
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
			var springProcess = new Process {StartInfo = startInfo};
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

		void ShowMissionSettings()
		{
			new MissionSettingsDialog().ShowDialog();
		}


		public void QuickSave()
		{
			if (SavePath != null)
			{
				Mission.SaveToXmlFile(SavePath);
			}
			else
			{
				SaveMission();
			}
		}

		void SaveMission()
		{

			var saveFileDialog = new SaveFileDialog
				{DefaultExt = WelcomeDialog.MissionExtension, Filter = WelcomeDialog.MissionDialogFilter, RestoreDirectory = true};
			if (saveFileDialog.ShowDialog() == true)
			{
				SavePath = saveFileDialog.FileName;
				Settings.Default.MissionPath = saveFileDialog.FileName;
				Settings.Default.Save();
				Mission.SaveToXmlFile(saveFileDialog.FileName);
			}
		}

		void BuildMission()
		{
			var filter = "Spring Mod Archive (*.sdz)|*.sdz|All files (*.*)|*.*";
			var saveFileDialog = new SaveFileDialog {DefaultExt = "sdz", Filter = filter, RestoreDirectory = true};
			if (saveFileDialog.ShowDialog() == true)
			{
				var loadingDialog = new LoadingDialog();
				loadingDialog.Text = "Building Mission";
				loadingDialog.Loaded += delegate
					{
						var mission = Mission;
						var fileName = saveFileDialog.FileName;
						Utils.InvokeInNewThread(delegate
							{
								mission.CreateArchive(fileName);
								var scriptPath = String.Format("{0}\\{1}.txt", Path.GetDirectoryName(fileName),
								                               Path.GetFileNameWithoutExtension(fileName));
								File.WriteAllText(scriptPath, mission.GetScript());
								this.Invoke(loadingDialog.Close);
							});
					};
				loadingDialog.ShowDialog();
			}
		}

		#region Nested type: MoveDirection

		enum MoveDirection
		{
			Up,
			Down
		}

		#endregion
	}
}