using System;
using System.Linq;
using System.Windows.Forms;
using JetBrains.Annotations;
using LobbyClient;
using PlasmaShared;
using SpringDownloader;
using SpringDownloader.Lines;

namespace SpringDownloader.MicroLobby
{
    public partial class PrivateMessageControl: UserControl
    {
        DateTime lastAnsweringMessageTime;
        public bool CanClose { get { return !Program.FriendManager.Friends.Contains(UserName); } }

        public string UserName { get; set; }
        public event EventHandler<EventArgs<string>> ChatLine { add { sendBox1.LineEntered += value; } remove { sendBox1.LineEntered -= value; } }

        public PrivateMessageControl(string name)
        {
            InitializeComponent();
            ChatBox.Font = Program.Conf.ChatFont;
            Name = name;
            UserName = name;
            ChatBox.MouseUp += autoscrollRichTextBox1_MouseUp;
            ChatBox.FocusInputRequested += (s, e) => GoToSendBox();
            HistoryManager.InsertLastLines(UserName, ChatBox);
            VisibleChanged += PrivateMessageControl_VisibleChanged;
            Program.TasClient.BattleUserJoined += TasClient_BattleUserJoined;
            Program.TasClient.UserAdded += TasClient_UserAdded;
            Program.TasClient.UserRemoved += TasClient_UserRemoved;
        }

        void TasClient_UserRemoved(object sender, TasEventArgs e)
        {
            var userName = e.ServerParams[0];
            if (userName != UserName) return;
            AddLine(new LeaveLine(userName));
        }

        void TasClient_UserAdded(object sender, TasEventArgs e)
        {
            var userName = e.ServerParams[0];
            if (userName != UserName) return;
            AddLine(new JoinLine(userName));
        }


        public void AddLine([NotNull] IChatLine line)
        {
            if (line == null) throw new ArgumentNullException("line");
            ChatBox.AddLine(line);
            HistoryManager.LogLine(UserName, line);
            var saidLine = line as SaidLine;
            if (saidLine != null && WindowsApi.IdleTime.TotalMinutes > 10 && (DateTime.Now - lastAnsweringMessageTime).TotalMinutes > 10)
            {
                if (saidLine.AuthorName != Program.TasClient.UserName)
                {
                    Program.TasClient.Say(TasClient.SayPlace.User, UserName, String.Format("Answering machine: I have been idle for {0} minutes.", (int)WindowsApi.IdleTime.TotalMinutes), false);
                    lastAnsweringMessageTime = DateTime.Now;
                }
            }
        }

        public void GoToSendBox()
        {
            sendBox1.Focus();
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
                        ContextMenus.GetPrivateMessageContextMenu(this).Show(this, me.Location);
                        return;
                    }
                }
            }

            if (me.Button == MouseButtons.Right) ContextMenus.GetPrivateMessageContextMenu(this).Show(this, me.Location);
        }

        void PrivateMessageControl_VisibleChanged(object sender, EventArgs e)
        {
            if (Visible) GoToSendBox();
            else ChatBox.ResetUnread();
        }


        void sendBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar != '\t') return;
            e.Handled = true;
        }

        void sendBox1_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            //Prevent cutting line in half when sending
            if (e.KeyCode == Keys.Return) sendBox1.SelectionStart = sendBox1.Text.Length;
        }

        void TasClient_BattleUserJoined(object sender, BattleUserEventArgs e1)
        {
            var userName = e1.UserName;
            if (userName != UserName) return;
            var joinedBattleID = e1.BattleID;
            var battle = Program.TasClient.ExistingBattles[joinedBattleID];
            AddLine(new JoinedBattleLine(userName, battle));
        }
    }
}