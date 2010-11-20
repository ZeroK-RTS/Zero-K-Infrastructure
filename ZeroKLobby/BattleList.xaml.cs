using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Forms;
using LobbyClient;
using PlasmaShared;
using Button = System.Windows.Controls.Button;
using UserControl = System.Windows.Controls.UserControl;

namespace ZeroKLobby.MicroLobby
{
  /// <summary>
  /// Interaction logic for BattleList.xaml
  /// </summary>
  public partial class BattleList: UserControl, INavigatable
  {
    CollectionViewSource view;

    public BattleList()
    {
      InitializeComponent();
    }

    public static IEnumerable<Battle> BattleWordFilter(IEnumerable<Battle> battles, string filter)
    {
      if (string.IsNullOrEmpty(filter)) return battles;
      var words = filter.ToUpper().Split(' ');
      return battles.Where(x => BattleWordFilter(x, words));
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
        if (string.IsNullOrEmpty(word)) continue; // dont filter empty words

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


    static bool FilterSpecialWordCheck(Battle battle, string word, out bool isMatch)
    {
      // mod shortcut 
      var knownGame = KnownGames.List.SingleOrDefault(x => x.Shortcut.ToUpper() == word);
      if (knownGame != null)
      {
        isMatch = battle.ModName != null && Regex.IsMatch(battle.ModName, knownGame.Regex);
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
            isMatch = Program.TasClient.ExistingUsers[battle.Founder].IsInGame;
            return true;
          case "FULL":
            isMatch = battle.NonSpectatorCount >= battle.MaxPlayers;
            return true;
        }
      }

      isMatch = false;
      return false;
    }

    void Refresh()
    {
      if (view != null) view.View.Refresh();
    }

    void ShowHostDialog(GameInfo filter)
    {
      using (var dialog = new HostDialog(filter))
      {
        if (dialog.ShowDialog() != DialogResult.OK) return;
        var springieCommands = dialog.SpringieCommands.Lines();

        ActionHandler.StopBattle();

        ActionHandler.SpawnAutohost(dialog.GameName,
                                    dialog.BattleTitle,
                                    dialog.Password,
                                    dialog.IsManageEnabled,
                                    dialog.MinPlayers,
                                    dialog.MaxPlayers,
                                    dialog.Teams,
                                    springieCommands);
      }
    }


    public string PathHead { get { return "battles"; } }

    public bool TryNavigate(params string[] path)
    {
      if (path.Length == 0) return false;
      if (path[0] != PathHead) return false;

      if (path.Length == 2)
      {
        if (!string.IsNullOrEmpty(path[1]))
        {
          var gameShortcut = path[1];
          tbFilter.Text = gameShortcut;
        }
        else tbFilter.Text = "";
      }
      return true;
    }

    public bool Hilite(HiliteLevel level, params string[] path)
    {
      return false;
    }

    public string GetTooltip(params string[] path)
    {
      return null;
    }

    void Button_Click(object sender, RoutedEventArgs e)
    {
      var battle = ((BattleIcon)((Button)sender).Tag).Battle;
      if (battle != null)
      {
        if (battle.Password != "*")
        {
          using (var form = new AskBattlePasswordForm(battle.Founder)) if (form.ShowDialog() == DialogResult.OK) ActionHandler.JoinBattle(battle.BattleID, form.Password);
        }
        else ActionHandler.JoinBattle(battle.BattleID, null);
      }
    }

    void RefreshEvent(object sender, EventArgs e)
    {
      Refresh();
    }

    bool isInitialized;
    void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
      if (Utils.IsDesignTime) return;
      if (!isInitialized)
      {
        isInitialized = true;
        view = (CollectionViewSource)Resources["view"];
        //tbFilter.Text = Program.Conf.BattleFilter;
        tbFilter.TextChanged += tbFilter_TextChanged; // this is needed after setting text
        view.Filter += view_Filter;
        view.Source = Program.BattleIconManager.BattleIcons;
        Program.TasClient.BattleInfoChanged += (s2, e2) => Refresh();
      }
    }

    void btnOpenNewBattle_Click(object sender, RoutedEventArgs e)
    {
      ShowHostDialog(KnownGames.GetDefaultGame());
    }

    void btnQuickJoin_Click(object sender, RoutedEventArgs e)
    {
      ActionHandler.StartQuickMatching(Program.Conf.BattleFilter);
    }

    void tbFilter_TextChanged(object sender, TextChangedEventArgs e)
    {
      Program.Conf.BattleFilter = tbFilter.Text;
      Refresh();
    }

    void view_Filter(object sender, FilterEventArgs e)
    {
      var battle = ((BattleIcon)e.Item).Battle;
      if (cbEmpty.IsChecked == false && battle.NonSpectatorCount == 0)
      {
        e.Accepted = false;
        return;
      }
      if (cbUnjoinable.IsChecked == false &&
          (battle.IsLocked || battle.IsPassworded || battle.NonSpectatorCount >= battle.MaxPlayers ||
           Program.TasClient.ExistingUsers[battle.Founder].IsInGame))
      {
        e.Accepted = false;
        return;
      }

      e.Accepted = BattleWordFilter(battle, tbFilter.Text.ToUpper().Split(' '));
    }


    /*
        protected override void OnMouseMove(MouseEventArgs e)
        {
          base.OnMouseMove(e);
          var battle = GetBattle(e.X, e.Y);
          var openGameButtonHit = OpenGameButtonHitTest(e.X, e.Y);
          Cursor = battle != null || openGameButtonHit ? Cursors.Hand : Cursors.Default;
          var cursorPoint = new Point(e.X, e.Y);
          if (cursorPoint == previousLocation) return;
          previousLocation = cursorPoint;

          UpdateTooltip(battle);
        }

     * void UpdateTooltip(Battle battle)
        {
          if (hoverBattle != battle)
          {
            hoverBattle = battle;
            if (battle != null) Program.ToolTip.SetBattle(this, battle.BattleID);
            else Program.ToolTip.SetText(this, null);
          }
        }
     * 
     *     void HandleBattle(object sender, EventArgs<BattleIcon> e)
    {
      var invalidate = view.Contains(e.Data);
      FilterBattles();
      Sort();
      var point = PointToClient(MousePosition);
      UpdateTooltip(GetBattle(point.X, point.Y));
      if (view.Contains(e.Data) || invalidate) Invalidate();
    }



        */
  }
}