using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using JetBrains.Annotations;
using LobbyClient;
using PlasmaShared;

namespace ZeroKLobby.MicroLobby
{
    public partial class BattleListControl: ScrollableControl
    {
        readonly Dictionary<BattleIcon, Point> battleIconPositions = new Dictionary<BattleIcon, Point>();
        object lastTooltip;
        readonly IEnumerable<BattleIcon> model;
        Point previousLocation;
        bool hideEmpty;
        bool sortByPlayers;
        bool hidePassworded;

        List<BattleIcon> view = new List<BattleIcon>();

        string filterText;
        bool hideFull;

        public string FilterText
        {
            get { return filterText; }
            set
            {
                filterText = value;
                Program.Conf.BattleFilter = value;
                Program.SaveConfig();
                FilterBattles();
                Sort();
                Invalidate();
            }
        }

        public BattleListControl()
        {
            InitializeComponent();
            AutoScroll = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.DoubleBuffer, true);
            BackColor = Color.White;
            FilterText = Program.Conf.BattleFilter;
            Disposed += BattleListControl_Disposed;
            Program.BattleIconManager.BattleAdded += HandleBattle;
            Program.BattleIconManager.BattleChanged += HandleBattle;
            Program.BattleIconManager.RemovedBattle += HandleBattle;
            model = Program.BattleIconManager.BattleIcons;
            sortByPlayers = true;
            hideEmpty = Program.Conf.HideEmptyBattles;
            hideFull = Program.Conf.HideNonJoinableBattles;
            hidePassworded = Program.Conf.HidePasswordedBattles;
            showOfficial = Program.Conf.ShowOfficialBattles;

            FilterBattles();
            Invalidate();
        }

        void BattleListControl_Disposed(object sender, EventArgs e)
        {
            Program.BattleIconManager.BattleChanged -= HandleBattle;
            Program.BattleIconManager.BattleAdded -= HandleBattle;
            Program.BattleIconManager.RemovedBattle -= HandleBattle;
        }


        public bool HideEmpty
        {
            get { return hideEmpty; }
            set
            {
                if (hideEmpty != value)
                {
                    hideEmpty = value;
                    Program.Conf.HideEmptyBattles = hideEmpty;
                    FilterBattles();
                    Sort();
                    Invalidate();
                }
            }
        }

        public bool ShowOfficial
        {
            get { return showOfficial; }
            set
            {
                if (showOfficial != value)
                {
                    showOfficial = value;
                    Program.Conf.ShowOfficialBattles = showOfficial;
                    FilterBattles();
                    Sort();
                    Invalidate();
                }
            }
        }


        public bool HideFull
        {
            get { return hideFull; }
            set
            {
                if (hideFull != value)
                {
                    hideFull = value;
                    Program.Conf.HideNonJoinableBattles = hideFull;
                    FilterBattles();
                    Sort();
                    Invalidate();
                }
            }
        }

        public bool HidePassworded
        {
            get { return hidePassworded; }
            set
            {
                if (hidePassworded != value)
                {
                    hidePassworded = value;
                    Program.Conf.HidePasswordedBattles = hidePassworded;
                    FilterBattles();
                    Sort();
                    Invalidate();
                }
            }
        }

        protected override void OnMouseDown([NotNull] MouseEventArgs e)
        {
            if (e == null) throw new ArgumentNullException("e");
            base.OnMouseDown(e);
            var battle = GetBattle(e.X, e.Y);
            if (e.Button == MouseButtons.Left)
            {
                if (battle != null)
                {
                    if (battle.Password != "*")
                    {
                        // hack dialog Program.FormMain
                        using (var form = new AskBattlePasswordForm(battle.Founder.Name)) if (form.ShowDialog() == DialogResult.OK) ActionHandler.JoinBattle(battle.BattleID, form.Password);
                    }
                    else ActionHandler.JoinBattle(battle.BattleID, null);
                }
                else if (OpenGameButtonHitTest(e.X, e.Y)) ShowHostDialog(KnownGames.GetDefaultGame());
            }
            else if (e.Button == MouseButtons.Right)
            {
                // todo - disable OnMouseMove while this is open to stop battle tooltips floating under it
                ContextMenus.GetBattleListContextMenu(battle).Show(this.Parent, e.Location);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            var battle = GetBattle(e.X, e.Y);
            var openBattleButtonHit = OpenGameButtonHitTest(e.X, e.Y);
            Cursor = battle != null || openBattleButtonHit ? Cursors.Hand : Cursors.Default;
            var cursorPoint = new Point(e.X, e.Y);
            if (cursorPoint == previousLocation) return;
            previousLocation = cursorPoint;

            if (openBattleButtonHit) UpdateTooltip("Host your own battle room\nBest for private games with friends");
            else UpdateTooltip(battle);
        }

        protected override void OnPaint(PaintEventArgs pe)
        {
            try
            {
                DpiMeasurement.DpiXYMeasurement();
                int scaledIconWidth = DpiMeasurement.ScaleValueX(BattleIcon.Width);
                int scaledIconHeight = DpiMeasurement.ScaleValueY(BattleIcon.Height);
                int scaledMapCellWidth = DpiMeasurement.ScaleValueX(BattleIcon.MapCellSize.Width);

                base.OnPaint(pe);
                var g = pe.Graphics;
                g.TranslateTransform(AutoScrollPosition.X, AutoScrollPosition.Y);
                battleIconPositions.Clear();
                var x = 0;
                var y = 0;


                g.DrawImage(ZklResources.border, x + DpiMeasurement.ScaleValueX(3), DpiMeasurement.ScaleValueY(3), DpiMeasurement.ScaleValueX(70), DpiMeasurement.ScaleValueY(70));
                g.DrawString("Open a new battle.", BattleIcon.TitleFont, BattleIcon.TextBrush,x + scaledMapCellWidth, y + DpiMeasurement.ScaleValueY(3));

                x += scaledIconWidth;

                foreach (var t in view)
                {
                    if (x + scaledIconWidth > Width)
                    {
                        x = 0;
                        y += scaledIconHeight;
                    }
                    battleIconPositions[t] = new Point(x, y);
                    if (g.VisibleClipBounds.IntersectsWith(new RectangleF(x, y, scaledIconWidth, scaledIconHeight))) g.DrawImageUnscaled(t.Image, x, y);
                    x += scaledIconWidth;
                }

                if (view.Count < model.Count())
                {
                    if (x + scaledIconWidth > Width)
                    {
                        x = 0;
                        y += scaledIconHeight;
                    }
                }

                AutoScrollMinSize = new Size(0, y + scaledIconHeight);
            }
            catch (Exception e)
            {
                Trace.WriteLine("Error in drawing battles: " + e);
            }
        }

        public static bool BattleWordFilter(Battle x, string[] words)
        {
            var hide = false;
            foreach (var wordIterated in words)
            {
                var word = wordIterated;
                var negation = false;
                if (word.StartsWith("-"))
                {
                    word = word.Substring(1);
                    negation = true;
                }
                if (String.IsNullOrEmpty(word)) continue; // dont filter empty words

                bool isSpecialWordMatch;
                if (FilterSpecialWordCheck(x, word, out isSpecialWordMatch)) // if word is mod shortcut, handle specially
                {
                    if ((!negation && !isSpecialWordMatch) || (negation && isSpecialWordMatch))
                    {
                        hide = true;
                        break;
                    }
                }
                else
                {
                    var playerFound = x.Users.Any(u => u.Name.ToUpper().Contains(word));
                    var titleFound = x.Title.ToUpper().Contains(word);
                    var modFound = x.ModName.ToUpper().Contains(word);
                    var mapFound = x.MapName.ToUpper().Contains(word);
                    if (!negation)
                    {
                        if (!(playerFound || titleFound || modFound || mapFound))
                        {
                            hide = true;
                            break;
                        }
                    }
                    else
                    {
                        if (playerFound || titleFound || modFound || mapFound) // for negation ignore players
                        {
                            hide = true;
                            break;
                        }
                    }
                }
            }
            return (!hide);
        }

        readonly Regex filterOrSplit = new Regex(@"\||\bOR\b");
        bool showOfficial = true;

        void FilterBattles()
        {
            if (model == null) return;
            view.Clear();
            if (String.IsNullOrEmpty(Program.Conf.BattleFilter)) view = model.ToList();
            else
            {
                var filterText = Program.Conf.BattleFilter.ToUpper();
                var orParts = filterOrSplit.Split(filterText);
                view = model.Where(icon => orParts.Any(filterPart => BattleWordFilter(icon.Battle, filterPart.Split(' ')))).ToList();
            }
            IEnumerable<BattleIcon> v = view; // speedup to avoid multiple "toList"
            if (hideEmpty) v = v.Where(bi => bi.Battle.NonSpectatorCount > 0);
            if (hideFull) v = v.Where(bi => bi.Battle.NonSpectatorCount < bi.Battle.MaxPlayers);
            if (showOfficial) v = v.Where(bi => bi.Battle.IsOfficial());
            if (hidePassworded) v = v.Where(bi => !bi.Battle.IsPassworded); 

            view = v.ToList();
        }

        static bool FilterSpecialWordCheck(Battle battle, string word, out bool isMatch)
        {
            // mod shortcut 
            var knownGame = KnownGames.List.SingleOrDefault(x => x.Shortcut.ToUpper() == word);
            if (knownGame != null)
            {
                isMatch = battle.ModName != null && knownGame.Regex.IsMatch(battle.ModName);
                return true;
            }
            else
            {
                switch (word)
                {
                    case "LOCK":
                        isMatch = battle.IsLocked;
                        return true;
                    case "PASSWORD":
                        isMatch = battle.Password != "*";
                        return true;
                    case "INGAME":
                        isMatch = battle.IsInGame;
                        return true;
                    case "FULL":
                        isMatch = battle.NonSpectatorCount >= battle.MaxPlayers;
                        return true;
                }
            }

            isMatch = false;
            return false;
        }

        Battle GetBattle(int x, int y)
        {
            x -= AutoScrollPosition.X;
            y -= AutoScrollPosition.Y;
            foreach (var kvp in battleIconPositions)
            {
                var battleIcon = kvp.Key;
                var position = kvp.Value;
                DpiMeasurement.DpiXYMeasurement(this);
                var battleIconRect = new Rectangle(position.X, position.Y, DpiMeasurement.ScaleValueX(BattleIcon.Width), DpiMeasurement.ScaleValueY(BattleIcon.Height));
                if (battleIconRect.Contains(x, y) && battleIcon.HitTest(x - position.X, y - position.Y)) return battleIcon.Battle;
            }
            return null;
        }

        bool OpenGameButtonHitTest(int x, int y)
        {
            x -= AutoScrollPosition.X;
            y -= AutoScrollPosition.Y;
            DpiMeasurement.DpiXYMeasurement(this);
            return x >  DpiMeasurement.ScaleValueX(3) && x <  DpiMeasurement.ScaleValueX(71) && y > DpiMeasurement.ScaleValueY(3) && y < DpiMeasurement.ScaleValueX(71);
        }

        

        public void ShowHostDialog(GameInfo filter)
        {
            using (var dialog = new HostDialog(filter))
            {
                if (dialog.ShowDialog() != DialogResult.OK) return;
                var springieCommands = dialog.SpringieCommands.Lines();

                ActionHandler.StopBattle();

                ActionHandler.SpawnAutohost(dialog.GameName,
                                            dialog.BattleTitle,
                                            dialog.Password,
                                            springieCommands);
            }
        }


        void Sort()
        {
            var ret = view.OrderByDescending(x => x.IsServerManaged);
            if (sortByPlayers) ret = ret.ThenByDescending(bi => bi.Battle.NonSpectatorCount);
            view = ret.ToList();
        }

        void UpdateTooltip(object tooltip)
        {
            if (lastTooltip != tooltip)
            {
                lastTooltip = tooltip;
                if (tooltip is Battle) Program.ToolTip.SetBattle(this, ((Battle)tooltip).BattleID);
                else Program.ToolTip.SetText(this, (string)tooltip);
            } 

        }

        void HandleBattle(object sender, EventArgs<BattleIcon> e)
        {
            var invalidate = view.Contains(e.Data);
            FilterBattles();
            Sort();
            var point = PointToClient(MousePosition);
            var battle = GetBattle(point.X, point.Y);
            if (battle != null) UpdateTooltip(battle);
            if (view.Contains(e.Data) || invalidate) Invalidate();
        }
    }
}