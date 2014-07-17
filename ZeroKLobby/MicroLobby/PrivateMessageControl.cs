using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using JetBrains.Annotations;
using LobbyClient;
using PlasmaShared;
using ZeroKLobby.Lines;

namespace ZeroKLobby.MicroLobby
{
  public partial class PrivateMessageControl: UserControl, INotifyPropertyChanged
  {
      public int encryptionState = 0;
      public SimpleCryptographicProvider encryptionInstance;
      private Timer timeoutTimer = new Timer();
      private const int keyExchangeTimeout = 10000; //in milisecond
      public const string encryptionSign = "\x2D0";
      private bool blockHistory = false;
      public const int isNoEncryption = 0;
      public const int isInitialRequest = 1;
      public const int isKeyExchange = 2;
      public const int isInEncryption = 3;
      public int myEncryptMsgCount = 0; //use to offset IV for encrypted message to avoid showing same ciphertext for same plaintext
      public int otherEncryptMsgCount = 0;

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

        timeoutTimer.Interval = keyExchangeTimeout; //maximum time to get reply for Key Exchange Request for encryption before reset everything
        timeoutTimer.Tick += timeoutTimer_Tick;
    }


    public void AddLine([NotNull] IChatLine line)
    {
      if (line == null) throw new ArgumentNullException("line");
      if ((line is SaidLine && Program.Conf.IgnoredUsers.Contains(((SaidLine)line).AuthorName)) ||
          (line is SaidExLine && Program.Conf.IgnoredUsers.Contains(((SaidExLine)line).AuthorName))) return;

      var saidLine = line as SaidLine;
      //encrypted "SaidLine" chat stuff:
      if (DoEncryptionProtocol(saidLine)) return; //some key-exchange protocol look too messy, this will hide them
      if (encryptionState == isInEncryption && (saidLine != null) && saidLine.Message.Substring(0, 1) == encryptionSign) //triangular semicolon:"ː" to differentiate normal message from encrypted message
      {
          if (saidLine.AuthorName != Program.TasClient.UserName)
          {
              saidLine.Message = encryptionInstance.AESDecryptFrom64Base(saidLine.Message.Substring(1), false, otherEncryptMsgCount); //decrypt and use expected security offset
              otherEncryptMsgCount += 1;
          }
          else saidLine.Message = encryptionInstance.AESDecryptFrom64Base(saidLine.Message.Substring(1), false, myEncryptMsgCount-1); //NOTE: my offset is counted-up from ChatTab.cs:ChatLine:LineEntered event
                    
          SecureSaidLine newSaidLine = new SecureSaidLine(saidLine.AuthorName, saidLine.Message, saidLine.Date); //minor format (coloring) tweak for encrypted line
          ChatBox.AddLine(newSaidLine);
          if (!blockHistory) HistoryManager.LogLine(UserName, newSaidLine);
      }
      else //normal (all) chat stuff:
      {
          ChatBox.AddLine(line);
          if (!blockHistory) HistoryManager.LogLine(UserName, line);
      }
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

    //ENCRYPTION SEGMENT:
    //the following code allow 2 users to negotiate exchange (sharing) of encryption key.
    public void StartEncryption(bool saveEncryptionSession)
    {
        if (encryptionState != isInitialRequest && (encryptionState == isInEncryption || encryptionState == isNoEncryption))
        {//A
            blockHistory = !saveEncryptionSession;
            Program.TasClient.Say(TasClient.SayPlace.User,
                                        UserName,
                                        "Encryption:RequestKeyExchange" + (blockHistory ? "(no history)" :""),
                                        false);
            encryptionState = isInitialRequest; //this ensure that if both A & B send "Encryption:RequestKeyExchange" at same time it will block the sequence and we have to do again with only 1 sender OR if sequence already progressed then previous step will be ignored
            timeoutTimer.Start(); //to auto cancel if no-reply for too long.
            myEncryptMsgCount = 0; //used to offset IV to avoid showing same ciphertext for same plaintext
            otherEncryptMsgCount = 0;
        }
    }

    private bool DoEncryptionProtocol(SaidLine line) //Return value indicate which line should be hidden (because it appear spammy)
    {
        try
        {
            if (line == null) return false; //is Null if line isn't actually a SaidLine type
            if (encryptionState != isInitialRequest && (encryptionState == isInEncryption || encryptionState == isNoEncryption) && (line.AuthorName != Program.TasClient.UserName) && (line.Message.StartsWith("Encryption:RequestKeyExchange")))
            {//B
                //Note: accept case when previous state was fully-encrypted (a reset/refresh of key) or not-encrypted (first time)
                blockHistory = line.Message.Contains("(no history)"); //2nd party should obey no history request too.
                Program.TasClient.Say(TasClient.SayPlace.User,
                                       UserName,
                                       "Encryption:Parameter:" + (blockHistory ? "(no history)" : ""),
                                       false);
                encryptionState = isInitialRequest;
                timeoutTimer.Start();
                myEncryptMsgCount = 0; //used to offset IV to avoid showing same ciphertext for same plaintext
                otherEncryptMsgCount = 0;
                return false;
            }
            else if (encryptionState != isKeyExchange && (encryptionState == isInitialRequest) && (line.AuthorName != Program.TasClient.UserName) && (line.Message.StartsWith("Encryption:Parameter:")))
            {//A
                encryptionInstance = new SimpleCryptographicProvider();
                encryptionInstance.InitializeRSAKeyPair(1024);
                string publicKey = encryptionInstance.GetRSAPublicKey();

                Program.TasClient.Say(TasClient.SayPlace.User,
                                        UserName,
                                        "Encryption:PublicKey:" + publicKey,
                                        false);
                encryptionState = isKeyExchange;
                return false;
            }
            else if (line.Message.StartsWith("Encryption:PublicKey:"))
            {
                if (encryptionState != isKeyExchange && (encryptionState == isInitialRequest) && (line.AuthorName != Program.TasClient.UserName))
                {//B
                    //Note: "(2 - encryptionState == 1)" mean it only accept if its progression upward from lower state (ensure no skipped sequence)
                    encryptionInstance = new SimpleCryptographicProvider();
                    string keyTxt = line.Message.Substring(21);
                    encryptionInstance.InitializeRSAPublicKey(keyTxt);
                    encryptionInstance.InitializeAESWith64BaseKey();

                    string symKey = encryptionInstance.RSAEncryptTo64Base(encryptionInstance.GetAES64BaseKey(), true);
                    string symIV = encryptionInstance.GetAES64BaseIV();

                    Program.TasClient.Say(TasClient.SayPlace.User,
                                            UserName,
                                            "Encryption:SymKey:" + symKey + " SymIV:" + symIV,
                                            false);
                    encryptionState = isKeyExchange;
                }
                return true;
            }
            else if (line.Message.StartsWith("Encryption:SymKey:"))
            {
                if (encryptionState != isInEncryption && (encryptionState == isKeyExchange) && (line.AuthorName != Program.TasClient.UserName))
                {//A
                    int symIVStart = line.Message.IndexOf(" SymIV:", 18);
                    string keyTxt = line.Message.Substring(18, symIVStart - 18);
                    keyTxt = encryptionInstance.RSADecryptFrom64Base(keyTxt, true);
                    string ivTxt = line.Message.Substring(symIVStart + 7);
                    encryptionInstance.InitializeAESWith64BaseKey(keyTxt, ivTxt);

                    Program.TasClient.Say(TasClient.SayPlace.User,
                                            UserName,
                                            "Encryption:Active",
                                            false);
                    encryptionState = isInEncryption;
                    timeoutTimer.Stop();
                }
                return true;
            }
            else if (encryptionState != isInEncryption && (encryptionState == isKeyExchange) && (line.Message.StartsWith("Encryption:Active")))
            {//B 
                //Note: A can also receive this with no harm but "encryptionState" check prevented this
                encryptionState = isInEncryption;
                timeoutTimer.Stop();
                return false;
            }
            else if (line.Message.StartsWith("Encryption:End"))
            {//A & B
                EndEncryption();
                if (line.AuthorName != Program.TasClient.UserName)
                {//B or A
                    Program.TasClient.Say(TasClient.SayPlace.User,
                                        UserName,
                                        "Encryption:Inactive",
                                        false);
                }
                return false;
            }
            return false;
        }
        catch (Exception e)
        {
            System.Diagnostics.Trace.TraceError("ERROR performing encryption key-exchange protocol: {0}", e.Message);
            return true;
        }
    }

    private void timeoutTimer_Tick(object sender, EventArgs e)
    {
        timeoutTimer.Stop();
        RequestEndEncryption("timeout");
    }

    public void EndEncryption()
    {
        if (encryptionInstance != null) encryptionInstance.Clear();
        encryptionInstance = null;
        encryptionState = isNoEncryption;
        blockHistory = false;
    }
    public void RequestEndEncryption(string reason=null)
    {
        Program.TasClient.Say(TasClient.SayPlace.User,
                            UserName,
                            "Encryption:End"+ (reason==null?"":" ("+reason+")"),
                            false);
        EndEncryption();
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