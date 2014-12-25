using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using JetBrains.Annotations;
using LobbyClient;
using ZkData;
using ZeroKLobby.Lines;

namespace ZeroKLobby.MicroLobby
{
  public partial class PrivateMessageControl: UserControl, INotifyPropertyChanged
  {
    DateTime lastAnsweringMessageTime;
    public bool CanClose { get { return !Program.FriendManager.Friends.Contains(UserName); } }


    public Label Label { get; set; }
    public string UserName { get; set; }
    public event EventHandler<EventArgs<string>> ChatLine { add { sendBox.LineEntered += value; } remove { sendBox.LineEntered -= value; } }

    public PrivateMessageControl(string name)
    {
        InitializeComponent();
        ChatBox.Font = Program.Conf.ChatFont;
        Name = name;
        UserName = name;
        ChatBox.MouseUp += autoscrollRichTextBox1_MouseUp;
        ChatBox.FocusInputRequested += (s, e) => GoToSendBox();
        ChatBox.ChatBackgroundColor = TextColor.background; //same as Program.Conf.BgColor but TextWindow.cs need this.
        ChatBox.IRCForeColor = 14; //mirc grey. Unknown use

        HistoryManager.InsertLastLines(UserName, ChatBox);
        
        VisibleChanged += PrivateMessageControl_VisibleChanged;
        Program.TasClient.BattleUserJoined += TasClient_BattleUserJoined;
        Program.TasClient.UserAdded += TasClient_UserAdded;
        Program.TasClient.UserRemoved += TasClient_UserRemoved;

        var extras = new BitmapButton();
        extras.Text = "Extras";
        extras.Click += (s, e) => { ContextMenus.GetPrivateMessageContextMenu(this).Show(extras, new Point(0, 0)); };
        ChatBox.Controls.Add(extras);

        sendBox.CompleteWord += (word) => //autocomplete of username
        {
            var w = word.ToLower();
            string[] nameInArray = new string[1]{name};
            System.Collections.Generic.IEnumerable<string> firstResult = nameInArray
                        .Where(x => x.ToLower().StartsWith(w))
                        .Union(nameInArray.Where(x => x.ToLower().Contains(w)));; 
            if (true)
            {
                ChatControl zkChatArea = Program.MainWindow.navigationControl.ChatTab.GetChannelControl("zk");
                if (zkChatArea != null)
                {
                    System.Collections.Generic.IEnumerable<string> extraResult = zkChatArea.playerBox.GetUserNames()
                        .Where(x => x.ToLower().StartsWith(w))
                        .Union(zkChatArea.playerBox.GetUserNames().Where(x => x.ToLower().Contains(w)));
                    firstResult = firstResult.Concat(extraResult); //Reference: http://stackoverflow.com/questions/590991/merging-two-ienumerablets
                }
            }
            return firstResult;
        };
    }


    public void AddLine([NotNull] IChatLine line)
    {
      if (line == null) throw new ArgumentNullException("line");
      if ((line is SaidLine && Program.Conf.IgnoredUsers.Contains(((SaidLine)line).AuthorName)) ||
          (line is SaidExLine && Program.Conf.IgnoredUsers.Contains(((SaidExLine)line).AuthorName))) return;

      ChatBox.AddLine(line);
      HistoryManager.LogLine(UserName, line);
      var saidLine = line as SaidLine;
      if (saidLine != null && WindowsApi.IdleTime.TotalMinutes > Program.Conf.IdleTime &&
          (DateTime.Now - lastAnsweringMessageTime).TotalMinutes > Program.Conf.IdleTime)
      {
        if (saidLine.AuthorName != Program.TasClient.UserName)
        {
          Program.TasClient.Say(TasClient.SayPlace.User,
                                UserName,
                                String.Format("Answering machine: I have been idle for {0} minutes.", (int)WindowsApi.IdleTime.TotalMinutes),
                                false);
          lastAnsweringMessageTime = DateTime.Now;
        }
      }
    }

    public void GoToSendBox()
    {
      sendBox.Focus();
    }

    public event PropertyChangedEventHandler PropertyChanged = delegate { };

    void PrivateMessageControl_VisibleChanged(object sender, EventArgs e)
    {
      if (Visible) GoToSendBox();
      else ChatBox.ResetUnread();
    }

    void TasClient_BattleUserJoined(object sender, BattleUserEventArgs e1)
    {
      var userName = e1.UserName;
      if (userName != UserName) return;
      var joinedBattleID = e1.BattleID;
      var battle = Program.TasClient.ExistingBattles[joinedBattleID];
      AddLine(new FriendJoinedBattleLine(userName, battle));
    }

    void TasClient_UserAdded(object sender, EventArgs<User> e)
    {
      var userName = e.Data.Name;
      if (userName != UserName) return;
      AddLine(new JoinLine(userName));
    }

    void TasClient_UserRemoved(object sender, TasEventArgs e)
    {
      var userName = e.ServerParams[0];
      if (userName != UserName) return;
      AddLine(new LeaveLine(userName));
    }

    void autoscrollRichTextBox1_MouseUp(object sender, MouseEventArgs me)
    {
      var word = ChatBox.HoveredWord.TrimEnd();

      if (word != null)
      {
        var user = Program.TasClient.ExistingUsers.Values.SingleOrDefault(x => x.Name.ToString().ToUpper() == word.ToUpper());
        if (user != null)
        {
          if (me.Button == MouseButtons.Right || !Program.Conf.LeftClickSelectsPlayer)
          {
            ContextMenus.GetPlayerContextMenu(user,false).Show(this, me.Location);
            return;
          }
        }
      }

      if (me.Button == MouseButtons.Right) ContextMenus.GetPrivateMessageContextMenu(this).Show(this, me.Location);
    }

    //void sendBox1_KeyPress(object sender, KeyPressEventArgs e) //we can handle TAB character fine
    //{
    //  if (e.KeyChar != '\t') return;
    //  e.Handled = true;
    //}

    //void sendBox1_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e) //Note: we turn off WordWrap so text should no longer be in multiple line
    //{
    //  //Prevent cutting line in half when sending
    //  if (e.KeyCode == Keys.Return) sendBox1.SelectionStart = sendBox1.Text.Length;
    //}
  }
}