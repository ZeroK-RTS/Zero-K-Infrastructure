using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using CMissionLib;
using CMissionLib.Actions;
using CMissionLib.Conditions;
using CMissionLib.UnitSyncLib;
using Microsoft.Win32;
using MissionEditor2.Properties;
using ZkData;
using Action = CMissionLib.Action;
using Condition = CMissionLib.Condition;
using Trigger = CMissionLib.Trigger;
using System.Reflection;
using System.Windows.Input;
using System.Windows.Media;
using Mission = CMissionLib.Mission;

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
        public System.Windows.Forms.TreeView LogicGrid2;

		public Mission Mission { get { return (Mission)GetValue(MissionProperty); } set { SetValue(MissionProperty, value); } }
		public string SavePath { get; set; }

        DispatcherTimer autosaveTimer;

        object draggedItem;
        Point dragStartPoint;
        bool isDragging;

		static MainWindow()
		{
			MissionProperty = DependencyProperty.Register("Mission", typeof(Mission), typeof(MainWindow));
		}


		public MainWindow()
		{
			Instance = this;
			InitializeComponent();
            this.Closing += MainWindow_Closing;
            this.autosaveTimer = new DispatcherTimer();
            autosaveTimer.Tick += new EventHandler(autosaveTimer_Tick);
            autosaveTimer.Interval = new TimeSpan(0, 5, 0);

            draggedItem = null;
            isDragging = false;
            dragStartPoint = new Point();
		}

		public void QuickSave()
		{
			if (SavePath != null) Mission.SaveToXmlFile(SavePath);
			else SaveMission();
		}

        public bool QuickSaveCheckSuccess()
        {
            if (SavePath != null) return Mission.SaveToXmlFile(SavePath);
            else return SaveMissionCheckSuccess();
        }

        public void AutoSave()
        {
            if (SavePath != null)
            {
                string dir = Path.GetDirectoryName(SavePath);
                string filename = Path.GetFileName(SavePath);
                Mission.SaveToXmlFile(Path.Combine(dir, "_autosave_" + filename), true);
            }
            else Mission.SaveToXmlFile(Path.Combine(Directory.GetCurrentDirectory(), "autosave.xml"), true);
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
			MenuItem menu = new MenuItem { Header = "New Action" };

            Dictionary<String, MenuItem> submenus = new Dictionary<String, MenuItem>
            {
                {"Logic", new MenuItem { Header = "Logic" }},
                {"Camera", new MenuItem { Header = "Camera" }},
                {"GUI", new MenuItem { Header = "GUI" }},
                {"Misc", new MenuItem { Header = "Misc." }},
            };
            foreach (var submenu in submenus)
            {
                menu.Items.Add(submenu.Value);
            }

			Action<string, Func<TriggerLogic>, string> addAction = (name, makeItem, submenuName) =>
				{
					var item = new MenuItem { Header = name };
                    var submenu = submenuName != null ? submenus[submenuName] : menu;
                    submenu.Items.Add(item);
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

            addAction("Cancel Countdown", () => new CancelCountdownAction(Mission.Countdowns.FirstOrDefault()), "Logic");
            addAction("Allow Unit Transfers", () => new AllowUnitTransfersAction(), "Logic");
            addAction("Create Units", () => new CreateUnitsAction(), "Logic");
            addAction("Destroy Units", () => new DestroyUnitsAction(), "Logic");
            addAction("Enable Triggers", () => new EnableTriggersAction(), "Logic");
            addAction("Disable Triggers", () => new DisableTriggersAction(), "Logic");
            addAction("Execute Triggers", () => new ExecuteTriggersAction(), "Logic");
            addAction("Execute Random Trigger", () => new ExecuteRandomTriggerAction(), "Logic");
            addAction("Give Orders", () => new GiveOrdersAction(), "Logic");
            //addAction("Give Targeted Orders", () => new GiveTargetedOrdersAction(), "Logic");
            addAction("Give Factory Orders", () => new GiveFactoryOrdersAction(), "Logic");
            addAction("Lock Units", () => new LockUnitsAction(), "Logic");
            addAction("Unlock Units", () => new UnlockUnitsAction(), "Logic");
            addAction("Make Units Always Visible", () => new MakeUnitsAlwaysVisibleAction(), "Logic");
            addAction("Make Units Neutral", () => new MakeUnitsNeutralAction(), "Logic");
            addAction("Modify Countdown", () => new ModifyCountdownAction(Mission.Countdowns.FirstOrDefault()), "Logic");
            addAction("Modify Counter", () => new ModifyCounterAction(), "Logic");
            addAction("Modify Resources", () => new ModifyResourcesAction(Mission.Players.First()), "Logic");
            addAction("Modify Score", () => new ModifyScoreAction(), "Logic");
            addAction("Modify Unit Health", () => new ModifyUnitHealthAction(), "Logic");
            addAction("Send Scores", () => new SendScoreAction(), "Logic");
            addAction("Start Countdown", () => new StartCountdownAction(GetNewCountdownName()), "Logic");
            addAction("Stop Cutscene Actions", () => new StopCutsceneActionsAction(), "Logic");
            addAction("Transfer Units", () => new TransferUnitsAction(Mission.Players.First()), "Logic");
            addAction("Victory", () => new VictoryAction(), "Logic");
            addAction("Defeat", () => new DefeatAction(), "Logic");

            addAction("Beauty Shot", () => new BeautyShotAction(), "Camera");
            addAction("Point Camera at Map Position", () => new SetCameraPointTargetAction(Mission.Map.Texture.Width / 2, Mission.Map.Texture.Height / 2), "Camera");
            addAction("Point Camera at Unit", () => new SetCameraUnitTargetAction(), "Camera");
            addAction("Set Camera Position/Direction", () => new SetCameraPosDirAction(), "Camera");
            addAction("Shake Camera", () => new ShakeCameraAction(), "Camera");
            addAction("Save Camera State", () => new SaveCameraStateAction(), "Camera");
            addAction("Restore Camera State", () => new RestoreCameraStateAction(), "Camera");

            addAction("Custom Action", () => new CustomAction(), "Misc");
            addAction("Custom Action (alternate)", () => new CustomAction2(), "Misc");
            addAction("Pause", () => new PauseAction(), "Misc");
            addAction("Play Music", () => new MusicAction(), "Misc");
            addAction("Play Looping Music", () => new MusicLoopAction(), "Misc");
            addAction("Stop Music", () => new StopMusicAction(), "Misc");
            addAction("Play Sound", () => new SoundAction(), "Misc");
			addAction("Sunrise", () => new SunriseAction(), "Misc");
			addAction("Sunset", () => new SunsetAction(), "Misc");
            addAction("Wait", () => new WaitAction(), "Misc");

            addAction("Add Objective", () => new AddObjectiveAction("newObj"), "GUI");
            addAction("Modify Objective", () => new ModifyObjectiveAction(), "GUI");
            addAction("Add Units to Objective", () => new AddUnitsToObjectiveAction(), "GUI");
            addAction("Add Point to Objective", () => new AddPointToObjectiveAction(Mission.Map.Texture.Width / 2, Mission.Map.Texture.Height / 2), "GUI");
            addAction("Enter Cutscene", () => new EnterCutsceneAction(), "GUI");
            addAction("Leave Cutscene", () => new LeaveCutsceneAction(), "GUI");
            addAction("Display Counters", () => new DisplayCountersAction(), "GUI");
            addAction("Fade In", () => new FadeInAction(), "GUI");
            addAction("Fade Out", () => new FadeOutAction(), "GUI");
            addAction("Show Console Message", () => new ConsoleMessageAction("Hello!"), "GUI");
            addAction("Show GUI Message", () => new GuiMessageAction("Hello!"), "GUI");
            addAction("Show GUI Message (Persistent)", () => new GuiMessagePersistentAction(GuiMessagePersistentDesc), "GUI");
            addAction("Hide GUI Message (Persistent)", () => new HideGuiMessagePersistentAction(), "GUI");
            addAction("Show Convo Message", () => new ConvoMessageAction("Hello! I am a talking head for missions! I require a custom widget to work!"), "GUI");
            addAction("Clear Convo Message Queue", () => new ClearConvoMessageQueueAction(), "GUI");
            addAction("Show Marker Point", () => new MarkerPointAction(Mission.Map.Texture.Width / 2, Mission.Map.Texture.Height / 2), "GUI");

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
            addAction("Cutscene Skipped", () => new CutsceneSkippedCondition());
			addAction("Game Ends", () => new GameEndedCondition());
            addAction("Game Preload", () => new GamePreloadCondition());
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
            addAction("Unit Entered LOS", () => new UnitEnteredLOSCondition());
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


		bool MoveCurrentItem(MoveDirection direction)
		{
            return MoveCurrentItem(direction, false);
		}

        bool MoveCurrentItem(MoveDirection direction, int count)
        {
            bool cont = true;
            for (int i = 0; i < count; i++)
            {
                cont = MoveCurrentItem(direction);
                if (cont == false) return false;
            }
            return true;
        }

        bool MoveCurrentItem(MoveDirection direction, bool toEnd)
        {
            if (logicGrid.SelectedItem is Trigger)
            {
                return MoveTrigger(direction, CurrentTrigger, toEnd);
            }
            else if (logicGrid.SelectedItem is TriggerLogic)
            {
                return MoveItem(direction, CurrentLogic, toEnd);
            }
            else if (logicGrid.SelectedItem == null) MessageBox.Show("No item selected.");
            else MessageBox.Show("Moving the selected item is not supported.");
            return false;
        }

		bool MoveItem(MoveDirection direction, TriggerLogic item)
		{
            return MoveItem(direction, item, false);
		}

        bool MoveItem(MoveDirection direction, TriggerLogic item, int count)
        {
            bool cont = true;
            for (int i = 0; i < count; i++)
            {
                cont = MoveItem(direction, item);
                if (cont == false) return false;
            }
            return true;
        }

        bool MoveItem(MoveDirection direction, TriggerLogic item, bool toEnd)
        {
            if (item == null) return false;
            var trigger = Mission.FindLogicOwner(item);
            var index = trigger.Logic.IndexOf(item);
            if (direction == MoveDirection.Up)
            {
                if (index == 0) return false;
                if (toEnd) trigger.Logic.Move(index, 0);
                else trigger.Logic.Move(index, index - 1);
            }
            else if (direction == MoveDirection.Down)
            {
                if (index + 2 > trigger.Logic.Count) return false;
                if (toEnd) trigger.Logic.Move(index, trigger.Logic.Count - 1);
                else trigger.Logic.Move(index, index + 1);
            }
            var displacedType = trigger.Logic[index];
            if ((displacedType is Action && item is Condition) ||
                (displacedType is Condition && item is Action)) MoveItem(direction, item);
            Mission.RaisePropertyChanged("AllLogic");
            return true;
        }

		bool MoveTrigger(MoveDirection direction, Trigger trigger)
		{
            return MoveTrigger(direction, trigger, false);
		}

        bool MoveTrigger(MoveDirection direction, Trigger trigger, int count)
        {
            bool cont = true;
            for (int i = 0; i < count; i++)
            {
                cont = MoveTrigger(direction, trigger);
                if (cont == false) return false;
            }
            return true;
        }

        bool MoveTrigger(MoveDirection direction, Trigger trigger, bool toEnd)
        {
            if (trigger == null) return false;
            var index = Mission.Triggers.IndexOf(trigger);
            if (direction == MoveDirection.Up)
            {
                if (index == 0) return false;
                if (toEnd) Mission.Triggers.Move(index, 0);
                else Mission.Triggers.Move(index, index - 1);
            }
            else if (direction == MoveDirection.Down)
            {
                if (index + 2 > Mission.Triggers.Count) return false;
                if (toEnd) Mission.Triggers.Move(index, Mission.Triggers.Count - 1);
                else Mission.Triggers.Move(index, index + 1);
            }
            Mission.RaisePropertyChanged("AllLogic");
            return true;
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
            SaveMissionCheckSuccess();
		}

        bool SaveMissionCheckSuccess()
        {
            var saveFileDialog = new SaveFileDialog { DefaultExt = WelcomeDialog.MissionExtension, Filter = WelcomeDialog.MissionDialogFilter, RestoreDirectory = true };
            var result = saveFileDialog.ShowDialog();
            if (result == true)
            {
                SavePath = saveFileDialog.FileName;
                Settings.Default.MissionPath = saveFileDialog.FileName;
                Settings.Default.Save();
                Mission.SaveToXmlFile(saveFileDialog.FileName);
                Mission.ModifiedSinceLastSave = false;
                return true;
            }
            return false;
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
                return;
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
                                RedirectStandardError = Debugger.IsAttached,
                                RedirectStandardOutput = Debugger.IsAttached,
			                	UseShellExecute = false
			                };
			var springProcess = new Process { StartInfo = startInfo };
            if (Debugger.IsAttached)
            {
                Utils.InvokeInNewThread(delegate
                    {
                        if (!springProcess.Start()) throw new Exception("Failed to start Spring");

                        System.Action readOut = async () =>
                            {
                                string line;
                                while((line = await springProcess.StandardOutput.ReadLineAsync()) != null)
                                    if (!String.IsNullOrEmpty(line)) Console.WriteLine(line);
                            };
                        readOut();

                        System.Action readErr = async () =>
                        {
                            string line;
                            while ((line = await springProcess.StandardError.ReadLineAsync()) != null)
                                if (!String.IsNullOrEmpty(line)) Console.WriteLine(line);
                        };
                        readErr();
                    });
            }
            else
            {
                springProcess.Start();
            }
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
            menu.AddAction("Move Up 5 Spaces", () => MoveItem(MoveDirection.Up, item, 5));
            menu.AddAction("Move to Top", () => MoveItem(MoveDirection.Up, item, true));
			menu.AddAction("Move Down", () => MoveItem(MoveDirection.Down, item));
            menu.AddAction("Move Down 5 Spaces", () => MoveItem(MoveDirection.Down, item, 5));
            menu.AddAction("Move to Bottom", () => MoveItem(MoveDirection.Down, item, true));
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

        void ConvoMessageButtonLoadedImg(object sender, RoutedEventArgs e)
        {
            var button = (Button)e.Source;
            button.Click += delegate
            {
                var filter = "Image Files(*.BMP;*.JPG;*.PNG)|*.BMP;*.JPG;*.PNG|All files (*.*)|*.*";
                var dialog = new OpenFileDialog { Filter = filter, RestoreDirectory = true };
                if (dialog.ShowDialog() == true)
                {
                    var action = (ConvoMessageAction)button.Tag;
                    action.ImagePath = dialog.FileName;
                }
            };
        }

        void ConvoMessageButtonLoadedSound(object sender, RoutedEventArgs e)
        {
            var button = (Button)e.Source;
            button.Click += delegate
            {
                var filter = "Sound Files(*.WAV;*.OGG)|*.WAV;*.OGG|All files (*.*)|*.*";
                var dialog = new OpenFileDialog { Filter = filter, RestoreDirectory = true };
                if (dialog.ShowDialog() == true)
                {
                    var action = (ConvoMessageAction)button.Tag;
                    action.SoundPath = dialog.FileName;
                }
            };
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
            menu.AddAction("Copy Trigger", () => Mission.CopyTrigger(trigger));
			menu.Items.Add(GetNewActionMenu(trigger));
			menu.Items.Add(GetNewConditionMenu(trigger));
			menu.Items.Add(new Separator());
			menu.AddAction("Move Up", () => MoveTrigger(MoveDirection.Up, trigger));
            menu.AddAction("Move Up 5 Spaces", () => MoveTrigger(MoveDirection.Up, trigger, 5));
            menu.AddAction("Move to Top", () => MoveTrigger(MoveDirection.Up, trigger, true));
			menu.AddAction("Move Down", () => MoveTrigger(MoveDirection.Down, trigger));
            menu.AddAction("Move Down 5 Spaces", () => MoveTrigger(MoveDirection.Down, trigger, 5));
            menu.AddAction("Move to Bottom", () => MoveTrigger(MoveDirection.Down, trigger, true));
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

        void NewMission()
        {
            MessageBoxResult result = ShowSaveWarning();
            if (result == MessageBoxResult.Cancel) return;
            else if (result == MessageBoxResult.Yes)
            {
                if (!QuickSaveCheckSuccess()) return;
            }
            WelcomeDialog.PromptForNewMission();
        }

        void LoadMission()
        {
            MessageBoxResult result = ShowSaveWarning();
            if (result == MessageBoxResult.Cancel) return;
            else if (result == MessageBoxResult.Yes)
            {
                if (!QuickSaveCheckSuccess()) return;
            }
            WelcomeDialog.AskForExistingMission();
        }

		void window_Loaded(object sender, RoutedEventArgs e)
		{
			var project = MainMenu.AddContainer("Project");
			project.AddAction("New", NewMission);
			project.AddAction("Open", LoadMission);
			project.AddAction("Save", QuickSave);
			project.AddAction("Save As", SaveMission);

            var welcomeScreen = new WelcomeDialog { ShowInTaskbar = true, Owner = this };
            welcomeScreen.ShowDialog();
            if (Mission == null)
            {
                MessageBox.Show("A mission needs to be selected");
                Environment.Exit(0);
            }

            var mission = MainMenu.AddContainer("Mission");
            mission.AddAction("Create Mutator", () => BuildMission());
            mission.AddAction("Create Invisible Mutator", () => BuildMission(true));
            mission.AddAction("Test Mission", TestMission);
            mission.AddAction("Publish", ShowMissionManagement);
            mission.AddAction("Settings", ShowMissionSettings);

            var newMenu = MainMenu.AddContainer("New");
            newMenu.AddAction("New Trigger", CreateNewTrigger);
            newMenu.AddAction("New Trigger (Repeating)", CreateNewRepeatingTrigger);
            newMenu.AddAction("New Region", CreateNewRegion);
            newMenu.AddAction("Copy Trigger", () => Mission.CopyTrigger(CurrentTrigger));
            newMenu.Items.Add(GetNewConditionMenu(delegate
            {
                if (logicGrid.SelectedItem is Trigger) return CurrentTrigger;
                if (logicGrid.SelectedItem is ActionsFolder) return CurrentActionsFolder.Trigger;
                if (logicGrid.SelectedItem is ConditionsFolder) return CurrentConditionsFolder.Trigger;
                if (logicGrid.SelectedItem is TriggerLogic) return Mission.FindLogicOwner(CurrentLogic);
                return null;
            }));
            newMenu.Items.Add(GetNewActionMenu(() => CurrentTrigger));

            var editMenu = MainMenu.AddContainer("Edit");
            editMenu.AddAction("Rename", RenameCurrentItem);
            editMenu.AddAction("Delete", DeleteCurrentItem);
            editMenu.AddAction("Move Up", () => MoveCurrentItem(MoveDirection.Up));
            editMenu.AddAction("Move Up 5 Spaces", () => MoveCurrentItem(MoveDirection.Up, 5));
            editMenu.AddAction("Move To Top", () => MoveCurrentItem(MoveDirection.Up, true));
            editMenu.AddAction("Move Down", () => MoveCurrentItem(MoveDirection.Down));
            editMenu.AddAction("Move Down 5 Spaces", () => MoveCurrentItem(MoveDirection.Down, 5));
            editMenu.AddAction("Move To Bottom", () => MoveCurrentItem(MoveDirection.Down, true));
            editMenu.AddAction("Expand All Triggers", ExpandAllTriggers);
            editMenu.AddAction("Collapse All Triggers", CollapseAllTriggers);

			var menu = new ContextMenu();
			menu.AddAction("New Trigger", CreateNewTrigger);
			menu.AddAction("New Trigger (Repeating)", CreateNewRepeatingTrigger);
			menu.AddAction("New Region", CreateNewRegion);
			logicGrid.ContextMenu = menu;

            Assembly assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
            string version = fileVersionInfo.FileVersion;
            var aboutString = String.Format("Mission Editor {0}\n\nby Quantum and KingRaptor\n\nFor help with the program, visit {1}", version, GlobalConst.BaseSiteUrl + "/Wiki/MissionEditorStartPage");
            var help = MainMenu.AddContainer("Help");
            help.AddAction("About", () => MessageBox.Show(aboutString, "About Mission Editor", MessageBoxButton.OK, MessageBoxImage.Information));

            autosaveTimer.Start();
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

        private bool GetVisualAbovePoint(Visual parent, Point pt, ref Visual last)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                // Retrieve child visual at specified index value.
                Visual childVisual = (Visual)VisualTreeHelper.GetChild(parent, i);
                if (childVisual is Border)
                {
                    // Do processing of the child visual object.
                    //int midY = (int)(((Border)childVisual).Height / 2);
                    Point childScreenPos = childVisual.PointToScreen(new Point(0, 0));
                    Point childPosInGrid = logicGrid.PointFromScreen(childScreenPos);
                    if (childPosInGrid.Y > pt.Y)
                    {
                        return true;
                    }
                    last = childVisual;
                }
                // Enumerate children of the child visual object.
                bool success = GetVisualAbovePoint(childVisual, pt, ref last);
                if (success) return true;
            }
            return false;
        }

        // implements drag and drop
        // goddamn WPF
        private void logicGrid_Drop(object sender, DragEventArgs e)
        {
            if (!isDragging || draggedItem == null) return;
            Point pt = e.GetPosition(logicGrid);
            Visual above = null;
            bool success = GetVisualAbovePoint(logicGrid, pt, ref above);
            bool handled = false;

            if (above == null)
            {
                if (draggedItem is Trigger)
                {
                    var myIndex = Mission.Triggers.IndexOf((Trigger)draggedItem);
                    Mission.Triggers.Move(myIndex, 0);
                    handled = true;
                }
                else if (draggedItem is Region)
                {
                    var myIndex = Mission.Regions.IndexOf((Region)draggedItem);
                    Mission.Regions.Move(myIndex, 0);
                    handled = true;
                }
            }
            else
            {
                Border border = (Border)above;
                object item = border.DataContext;
                if (draggedItem == item)
                {
                    // do nothing
                }
                else if (draggedItem is Trigger)
                {
                    var myIndex = Mission.Triggers.IndexOf((Trigger)draggedItem);

                    Trigger targetTrigger = null;
                    if (item is Trigger)
                    {
                        targetTrigger = (Trigger)item;
                    }
                    else if (item is ActionsFolder)
                    {
                        targetTrigger = ((ActionsFolder)item).Trigger;
                    }
                    else if (item is ConditionsFolder)
                    {
                        targetTrigger = ((ConditionsFolder)item).Trigger;
                    }
                    else if (item is TriggerLogic)
                    {
                        targetTrigger = Mission.FindLogicOwner((TriggerLogic)item);
                    }

                    if (targetTrigger == null)
                    {
                        var newIndex = (item is Region) ? Mission.Triggers.Count - 1 : 0;
                        Mission.Triggers.Move(myIndex, newIndex);
                        handled = true;
                    }
                    else
                    {
                        var newIndex = Mission.Triggers.IndexOf(targetTrigger);
                        if(newIndex != myIndex) Mission.Triggers.Move(myIndex, newIndex);
                        handled = true;
                    }
                }
                else if (draggedItem is TriggerLogic)
                {
                    var myTrigger = Mission.FindLogicOwner((TriggerLogic)draggedItem);
                    var myIndex = myTrigger.Logic.IndexOf((TriggerLogic)draggedItem);

                    object hit = logicGrid.InputHitTest(pt);
                    Border hitBorder = null;
                    if (hit is Border) hitBorder = (Border)hit;
                    else if (hit is TextBlock) hitBorder = (Border)((TextBlock)hit).Parent;
                    bool directDropIntoBin = (hitBorder != null) && (item == hitBorder.DataContext);

                    Trigger targetTrigger = null;
                    int newIndex = 0;

                    if (item is Trigger)
                    {
                        targetTrigger = (Trigger)item;
                        newIndex = targetTrigger.Logic.Count - 1;
                    }
                    else if (item is ActionsFolder && draggedItem is Action)
                    {
                        targetTrigger = ((ActionsFolder)item).Trigger;
                    }
                    else if (item is ConditionsFolder && draggedItem is Condition)
                    {
                        targetTrigger = ((ConditionsFolder)item).Trigger;
                    }
                    else if (item is Action && draggedItem is Action)
                    {
                        targetTrigger = Mission.FindLogicOwner((TriggerLogic)item);
                        newIndex = targetTrigger.Logic.IndexOf((Action)item);
                    }
                    else if (item is Condition && draggedItem is Condition)
                    {
                        targetTrigger = Mission.FindLogicOwner((TriggerLogic)item);
                        newIndex = targetTrigger.Logic.IndexOf((Condition)item);
                    }

                    if (targetTrigger == null)
                    {
                        // do nothing
                    }
                    else if (targetTrigger != myTrigger)
                    {
                        targetTrigger.Logic.Add((TriggerLogic)draggedItem);
                        if(!directDropIntoBin) targetTrigger.Logic.Move(targetTrigger.Logic.Count - 1, newIndex);
                        myTrigger.Logic.Remove((TriggerLogic)draggedItem);
                        handled = true;
                    }
                    else if (newIndex != myIndex)
                    {
                        targetTrigger.Logic.Move(myIndex, newIndex);
                        handled = true;
                    }
                }
                else if (draggedItem is Region)
                {
                    var myIndex = Mission.Regions.IndexOf((Region)draggedItem);
                    Region targetRegion = null;
                    if (item is Region)
                    {
                        targetRegion = (Region)item;
                    }

                    if (targetRegion == null)
                    {
                        Mission.Regions.Move(myIndex, 0);
                        handled = true;
                    }
                    else
                    {
                        var newIndex = Mission.Regions.IndexOf(targetRegion);
                        if (newIndex != myIndex) Mission.Regions.Move(myIndex, newIndex);
                        handled = true;
                    }
                }
            }
            e.Handled = handled;
            draggedItem = null;
            isDragging = false;
        }

        private void logicGrid_MouseUp(object sender, MouseEventArgs e)
        {
            draggedItem = null;
            isDragging = false;
        }

        private void logicGrid_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && !isDragging && draggedItem != null)
            {
                Point position = e.GetPosition(logicGrid);
                if (Math.Abs(position.Y - dragStartPoint.Y) > SystemParameters.MinimumVerticalDragDistance || 
                    Math.Abs(position.X - dragStartPoint.X) > SystemParameters.MinimumHorizontalDragDistance)
                {
                    isDragging = true;
                    DragDrop.DoDragDrop(logicGrid, draggedItem, DragDropEffects.Move);
                }
            }  
        }
        
        private void logicGrid_PreviewMouseLeftButtonDown(object sender, MouseEventArgs e)
        {
            Point pt = e.GetPosition(logicGrid);
            object hit = logicGrid.InputHitTest(pt);
            Border hitBorder = null;
            if (hit is Border) hitBorder = (Border)hit;
            else if (hit is TextBlock) hitBorder = (Border)((TextBlock)hit).Parent;
            else return;

            isDragging = false;
            draggedItem = hitBorder.DataContext;
            dragStartPoint = pt;
        }

        // manual drag and drop implementation - ugh
        /*
        private void triggerItem_Drop(object sender, DragEventArgs e)
        {
            if (draggedItem == null) return;

            bool handled = false;
            bool toEnd = true;
            Point pt = e.GetPosition(logicGrid);
            object hit = logicGrid.InputHitTest(pt);
            Border border;
            if (hit is Border) border = (Border)e.Source;
            else if (hit is TextBlock) border = (Border)((TextBlock)e.Source).Parent;
            // dropped on to the grid; if this is a trigger or region, move to the end of its list
            else if (hit == logicGrid)
            {
                if (draggedItem is Trigger)
                {
                    var myIndex = Mission.Triggers.IndexOf((Trigger)draggedItem);
                    Mission.Triggers.Move(myIndex, Mission.Triggers.Count);
                    handled = true;
                }
                draggedItem = null;
                return;
            }
            else
            {
                MessageBox.Show(string.Format("{0}, {1}", hit.ToString(), hit.GetType()));  //debug
                draggedItem = null;
                return;
            }

            object hitDataContext = border.DataContext;

            if (draggedItem is Trigger)
            {
                var myIndex = Mission.Triggers.IndexOf((Trigger)draggedItem);

                foreach (object item in logicGrid.Items)
                {
                    if (item == hitDataContext)
                    {
                        toEnd = false;

                        Trigger target = (Trigger)draggedItem;
                        if (item is Trigger)
                        {
                            target = (Trigger)item;
                        }
                        else if (item is ActionsFolder)
                        {
                            target = ((ActionsFolder)item).Trigger;
                        }
                        else if (item is ConditionsFolder)
                        {
                            target = ((ConditionsFolder)item).Trigger;
                        }
                        else if (item is TriggerLogic)
                        {
                            target = Mission.FindLogicOwner((TriggerLogic)item);
                        }
                        if (target != draggedItem)
                        {
                            var newIndex = Mission.Triggers.IndexOf(target);
                            Mission.Triggers.Move(myIndex, newIndex);
                            handled = true;
                            break;
                        }
                    }
                }
                
                if (!handled)
                {
                    //Mission.Triggers.Move(myIndex, Mission.Triggers.Count - 1);
                    //handled = true;
                }
            }
            else if (draggedItem is TriggerLogic)
            {
                var myTrigger = Mission.FindLogicOwner((TriggerLogic)draggedItem);
                var myIndex = myTrigger.Logic.IndexOf((TriggerLogic)draggedItem);

                foreach (object item in logicGrid.Items)
                {
                    if (item == hitDataContext)
                    {
                        Trigger targetTrigger = (Trigger)item;
                        int newIndex = myIndex;

                        if (item is Trigger)
                        {
                            targetTrigger = (Trigger)item;
                        }
                        else if (item is ActionsFolder && draggedItem is Action)
                        {
                            targetTrigger = ((ActionsFolder)item).Trigger;
                        }
                        else if (item is ConditionsFolder && draggedItem is Condition)
                        {
                            targetTrigger = ((ConditionsFolder)item).Trigger;
                        }
                        else if (item is Action && draggedItem is Action)
                        {
                            targetTrigger = Mission.FindLogicOwner((TriggerLogic)item);
                            newIndex = targetTrigger.Logic.IndexOf((Action)item);
                            toEnd = false;
                        }
                        else if (item is Condition && draggedItem is Condition)
                        {
                            targetTrigger = Mission.FindLogicOwner((TriggerLogic)item);
                            newIndex = targetTrigger.Logic.IndexOf((Condition)item);
                            toEnd = false;
                        }

                        //MessageBox.Show(string.Format("{0}, {1}, {2}", targetTrigger == myTrigger, myIndex, newIndex));  //debug
                        if (targetTrigger != myTrigger)
                        {
                            if (toEnd) targetTrigger.Logic.Add((TriggerLogic)draggedItem);
                            else targetTrigger.Logic.Insert(newIndex, (TriggerLogic)draggedItem);
                            myTrigger.Logic.Remove((TriggerLogic)draggedItem);
                            handled = true;
                            break;
                        }
                        else if(newIndex != myIndex)
                        {
                            targetTrigger.Logic.Move(myIndex, newIndex);
                            handled = true;
                            break;
                        }
                    }
                }
            }
            e.Handled = handled;
            if (handled) Mission.RaisePropertyChanged("AllLogic");
            draggedItem = null;
        }*/

		void DeleteRegion(Region region)
		{
			Mission.Regions.Remove(region);
		}

        void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (true)   //(Mission.ModifiedSinceLastSave)
            {
                MessageBoxResult result = ShowSaveWarning();
                if (result == MessageBoxResult.Cancel)
                {
                    e.Cancel = true;
                }
                else if (result == MessageBoxResult.Yes)
                {
                    e.Cancel = !QuickSaveCheckSuccess();
                }
            }
        }

        void autosaveTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                //MessageBox.Show("Autosaving...", "Autosaving", MessageBoxButton.OK);
                autosaveTimer.Stop();
                AutoSave();
            }
            catch(Exception ex)
            {
                throw ex;
            }
            finally
            {
                autosaveTimer.Start();
            }
        }

        MessageBoxResult ShowSaveWarning(bool exit = false)
        {
            string msg = exit ? "You will lose any unsaved changes, save before exiting?" : "You will lose any unsaved changes, save first?";
            return MessageBox.Show(msg, "Mission Editor", MessageBoxButton.YesNoCancel, MessageBoxImage.Exclamation);
        }
    }
}