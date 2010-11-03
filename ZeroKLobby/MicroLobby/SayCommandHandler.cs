using System;
using System.Windows.Forms;
using LobbyClient;
using ZeroKLobby;

namespace ZeroKLobby.MicroLobby
{
    public class SayCommandHandler
    {
        public SayCommandHandler(TasClient client)
        {
            client.Saying += client_Saying;
        }

        void client_Saying(object sender, SayingEventArgs e)
        {
            if (e.Text.Length > 0 && e.Text[0] == '/')
            {
                if (e.Text.StartsWith("/me"))
                {
                    e.Text = e.Text.Substring(4);
                    e.IsEmote = true;
                }
                else
                {
                    e.Cancel = true;
                    var words = e.Text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if ((words[0] == "/j" || words[0] == "/join" || words[0] == "/channel") && words.Length > 1)
                    {
                        if (words.Length == 2) Program.TasClient.JoinChannel(words[1].Replace("#", String.Empty));
                        else
                        {
                            Program.TasClient.JoinChannel(words[1].Replace("#", String.Empty), words[2]);
                            Program.AutoJoinManager.AddPassword(words[1], words[2]);
                        }
                    }
                    else if (words[0] == "/p" || words[0] == "/part" || words[0] == "/l" || words[0] == "/leave") Program.TasClient.LeaveChannel(e.Channel);
                    else if ((words[0] == "/pm" || words[0] == "/msg" || words[0] == "/message" || words[0] == "/w") && words.Length > 2) Program.TasClient.Say(TasClient.SayPlace.User, words[1], PlasmaShared.Utils.Glue(words, 2), false);
                    else if (words[0] == "/disconnect") Program.TasClient.Disconnect();
                    else if (words[0] == "/rename" || words[0] == "/nick")
                    {
                        Program.TasClient.RenameAccount(words[1]);
                        MessageBox.Show("Renaming account to: " + words[1] + ".\r\n The server will disconnect you now.");
                    }
                    else if (words[0] == "/raw") Program.TasClient.SendRaw(PlasmaShared.Utils.Glue(words, 1));
					else if (words[0] == "/help") NavigationControl.Instance.Path = "help";
                    else
											MainWindow.Instance.NotifyUser("server", "Command not recognized");  
                }
            }
        }
    }
}