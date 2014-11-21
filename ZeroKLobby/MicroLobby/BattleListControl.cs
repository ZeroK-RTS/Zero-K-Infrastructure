using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
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
        Point openBattlePosition;
        readonly Regex filterOrSplit = new Regex(@"\||\bOR\b");
        string filterText;
        bool hideEmpty;
        bool hideFull;
        bool hidePassworded;
        object lastTooltip;
        readonly IEnumerable<BattleIcon> model;
        Point previousLocation;
        bool showOfficial = true;
        readonly bool sortByPlayers;

        List<BattleIcon> view = new List<BattleIcon>();
        static Pen dividerPen = new Pen(Color.DarkCyan, 3) {DashStyle = DashStyle.Dash};
        static Font dividerFont = new Font("Segoe UI", 15.25F, FontStyle.Bold);
        static SolidBrush dividerFontBrush = new SolidBrush(Program.Conf.TextColor);


        public string FilterText
        {
            get { return filterText; }
            set
            {
                filterText = value;
                Program.Conf.BattleFilter = value;
                Program.SaveConfig();
                Repaint();
            }
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
                    Repaint();
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
                    Repaint();
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
                    Repaint();
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
                    Repaint();
                }
            }
        }

        public BattleListControl()
        {
            InitializeComponent();
            AutoScroll = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.DoubleBuffer, true);
            BackColor = Program.Conf.BgColor;
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

            Repaint();
        }

        void BattleListControl_Disposed(object sender, EventArgs e)
        {
            Program.BattleIconManager.BattleChanged -= HandleBattle;
            Program.BattleIconManager.BattleAdded -= HandleBattle;
            Program.BattleIconManager.RemovedBattle -= HandleBattle;
        }

        public static bool BattleWordFilter(Battle x, string[] words)
        {
            bool hide = false;
            foreach (string wordIterated in words)
            {
                string word = wordIterated;
                bool negation = false;
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
                    bool playerFound = x.Users.Any(u => u.Name.ToUpper().Contains(word));
                    bool titleFound = x.Title.ToUpper().Contains(word);
                    bool modFound = x.ModName.ToUpper().Contains(word);
                    bool mapFound = x.MapName.ToUpper().Contains(word);
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

        public void ShowHostDialog(GameInfo filter)
        {
            using (var dialog = new HostDialog(filter))
            {
                if (dialog.ShowDialog() != DialogResult.OK) return;
                string[] springieCommands = dialog.SpringieCommands.Lines();

                ActionHandler.StopBattle();

                ActionHandler.SpawnAutohost(dialog.GameName, dialog.BattleTitle, dialog.Password, springieCommands);
            }
        }

        protected override void OnMouseDown([NotNull] MouseEventArgs e)
        {
            if (e == null) throw new ArgumentNullException("e");
            base.OnMouseDown(e);
            Battle battle = GetBattle(e.X, e.Y);
            if (e.Button == MouseButtons.Left)
            {
                if (battle != null)
                {
                    if (battle.Password != "*")
                    {
                        // hack dialog Program.FormMain
                        using (var form = new AskBattlePasswordForm(battle.Founder.Name)) if (form.ShowDialog() == DialogResult.OK) ActionHandler.JoinBattle(battle.BattleID, form.Password);
                    } else 
                    {
                        if (battle.IsLocked) ActionHandler.JoinBattleSpec(battle.BattleID);
                        else ActionHandler.JoinBattle(battle.BattleID, null);    

                    }
                    
                }
                else if (OpenGameButtonHitTest(e.X, e.Y)) ShowHostDialog(KnownGames.GetDefaultGame());
            }
            else if (e.Button == MouseButtons.Right)
            {
                // todo - disable OnMouseMove while this is open to stop battle tooltips floating under it
                ContextMenus.GetBattleListContextMenu(battle).Show(Parent, e.Location);
                Program.ToolTip.Visible = false;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            Battle battle = GetBattle(e.X, e.Y);
            bool openBattleButtonHit = OpenGameButtonHitTest(e.X, e.Y);
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
                Graphics g = pe.Graphics;
                g.TranslateTransform(AutoScrollPosition.X, AutoScrollPosition.Y);
                battleIconPositions.Clear();
                int x = 0;
                int y = 0;

                
                PaintDivider(g, ref x, ref y, "Match maker queues");
                foreach (BattleIcon t in view.Where(b=>b.Battle.IsQueue && !b.IsInGame))
                {
                    if (x + scaledIconWidth > Width)
                    {
                        x = 0;
                        y += scaledIconHeight;
                    }
                    PainBattle(t, g, ref x, ref y, scaledIconWidth, scaledIconHeight);
                }

                x = 0;
                y += scaledIconHeight;

                PaintDivider(g, ref x, ref y, "Custom battles");
                PainOpenBattleButton(g, ref x, ref y, scaledMapCellWidth, scaledIconWidth);

                foreach (BattleIcon t in view.Where(b => !b.Battle.IsQueue && !b.IsInGame))
                {
                    if (x + scaledIconWidth > Width)
                    {
                        x = 0;
                        y += scaledIconHeight;
                    }
                    PainBattle(t, g, ref x, ref y, scaledIconWidth, scaledIconHeight);
                }
                x = 0;
                y += scaledIconHeight;

                PaintDivider(g, ref x, ref y, "Games in progress");

                foreach (BattleIcon t in view.Where(b => b.IsInGame))
                {
                    if (x + scaledIconWidth > Width)
                    {
                        x = 0;
                        y += scaledIconHeight;
                    }
                    PainBattle(t, g, ref x, ref y, scaledIconWidth, scaledIconHeight);
                }


                AutoScrollMinSize = new Size(0, y + scaledIconHeight);
            }
            catch (Exception e)
            {
                Trace.WriteLine("Error in drawing battles: " + e);
            }
        }

        void PaintDivider(Graphics g, ref int x, ref int y, string text)
        {
            y += 3;

            g.DrawLine(dividerPen, 5, y + 2, Width - 10, y + 2);
            y += 4;
            g.DrawString(text, dividerFont, dividerFontBrush,new RectangleF(10,y , Width-20,30),new StringFormat()
            {
                LineAlignment = StringAlignment.Center,Alignment = StringAlignment.Center
            }  );
            y += 24;
            g.DrawLine(dividerPen, 5, y + 2, Width - 10, y + 2);
            y += 4;
        }

        void PainOpenBattleButton(Graphics g, ref int x, ref int y, int scaledMapCellWidth, int scaledIconWidth)
        {
            g.DrawImage(ZklResources.border,
                x + DpiMeasurement.ScaleValueX(3),
                y + DpiMeasurement.ScaleValueY(3),
                DpiMeasurement.ScaleValueX(70),
                DpiMeasurement.ScaleValueY(70));
            g.DrawString("Open a new battle.", BattleIcon.TitleFont, BattleIcon.TextBrush, x + scaledMapCellWidth, y + DpiMeasurement.ScaleValueY(3));
            openBattlePosition = new Point(x, y);
            x += scaledIconWidth;
        }

        void PainBattle(BattleIcon t, Graphics g, ref int x, ref int y, int scaledIconWidth, int scaledIconHeight)
        {
            battleIconPositions[t] = new Point(x, y);
            if (g.VisibleClipBounds.IntersectsWith(new RectangleF(x, y, scaledIconWidth, scaledIconHeight))) g.DrawImageUnscaled(t.Image, x, y);
            x += scaledIconWidth;
        }




        void FilterBattles()
        {
            if (model == null) return;
            view.Clear();
            if (String.IsNullOrEmpty(Program.Conf.BattleFilter)) view = model.ToList();
            else
            {
                string filterText = Program.Conf.BattleFilter.ToUpper();
                string[] orParts = filterOrSplit.Split(filterText);
                view = model.Where(icon => orParts.Any(filterPart => BattleWordFilter(icon.Battle, filterPart.Split(' ')))).ToList();
            }
            IEnumerable<BattleIcon> v = view; // speedup to avoid multiple "toList"
            if (hideEmpty) v = v.Where(bi => bi.Battle.NonSpectatorCount > 0 || bi.Battle.IsQueue);
            if (hideFull) v = v.Where(bi => bi.Battle.NonSpectatorCount < bi.Battle.MaxPlayers);
            if (showOfficial) v = v.Where(bi => bi.Battle.IsOfficial());
            if (hidePassworded) v = v.Where(bi => !bi.Battle.IsPassworded);

            view = v.ToList();
        }

        static bool FilterSpecialWordCheck(Battle battle, string word, out bool isMatch)
        {
            // mod shortcut 
            GameInfo knownGame = KnownGames.List.SingleOrDefault(x => x.Shortcut.ToUpper() == word);
            if (knownGame != null)
            {
                isMatch = battle.ModName != null && knownGame.Regex.IsMatch(battle.ModName);
                return true;
            }
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

            isMatch = false;
            return false;
        }

        Battle GetBattle(int x, int y)
        {
            x -= AutoScrollPosition.X;
            y -= AutoScrollPosition.Y;
            foreach (var kvp in battleIconPositions)
            {
                BattleIcon battleIcon = kvp.Key;
                Point position = kvp.Value;
                DpiMeasurement.DpiXYMeasurement(this);
                var battleIconRect = new Rectangle(position.X,
                    position.Y,
                    DpiMeasurement.ScaleValueX(BattleIcon.Width),
                    DpiMeasurement.ScaleValueY(BattleIcon.Height));
                if (battleIconRect.Contains(x, y) && battleIcon.HitTest(x - position.X, y - position.Y)) return battleIcon.Battle;
            }
            return null;
        }

        bool OpenGameButtonHitTest(int x, int y)
        {
            x -= AutoScrollPosition.X;
            y -= AutoScrollPosition.Y;
            DpiMeasurement.DpiXYMeasurement(this);
            return x > openBattlePosition.X + DpiMeasurement.ScaleValueX(3) && x < openBattlePosition.X + DpiMeasurement.ScaleValueX(71) && y > openBattlePosition.Y + DpiMeasurement.ScaleValueY(3) &&
                   y < openBattlePosition.Y + DpiMeasurement.ScaleValueX(71);
        }

        void Repaint()
        {
            FilterBattles();
            Sort();
            Invalidate();
        }


        void Sort()
        {
            IOrderedEnumerable<BattleIcon> ret = view.OrderBy(x=>x.Battle.IsInGame);
            if (sortByPlayers) ret = ret.ThenByDescending(bi => bi.Battle.NonSpectatorCount);
            ret = ret.ThenBy(x => x.Battle.Title);
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
            bool invalidate = view.Contains(e.Data);
            FilterBattles();
            Sort();
            Point point = PointToClient(MousePosition);
            Battle battle = GetBattle(point.X, point.Y);
            if (battle != null) UpdateTooltip(battle);
            if (view.Contains(e.Data) || invalidate) Invalidate();
        }
    }

    public interface IRenderElement
    {
        void RenderAtPosition(double x, double y);
        double DimensionX { get; }
        double DimensionY { get; }


    }
}