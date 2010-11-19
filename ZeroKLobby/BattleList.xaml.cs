using System.Windows;
using System.Windows.Data;
using System.Windows.Forms;
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

    public string PathHead { get { return "battles"; } }

    public bool TryNavigate(params string[] path)
    {
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

    void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
      view = (CollectionViewSource)Resources["view"]; // todo remove when know how;
      view.Filter += view_Filter;
      view.Source = Program.BattleIconManager.BattleIcons;
      
    }

    void btnMore_Click(object sender, RoutedEventArgs e)
    {
      view.View.Refresh();
    }

    void view_Filter(object sender, FilterEventArgs e)
    {
      e.Accepted = BattleListControl.BattleWordFilter(((BattleIcon)e.Item).Battle, tbFilter.Text.ToUpper().Split(' '));
    }

    private void tbFilter_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
      view.View.Refresh();
    }
  }
}