using System;
using LobbyClient;
using ZeroKLobby.MicroLobby;

namespace ZeroKLobby.Lines
{
    class JoinedBattleLine: IChatLine
    {
        protected Battle Battle { get; private set; }
        public DateTime Date { get; private set; }
        public string UserName { get; private set; }

        public JoinedBattleLine(string userName, Battle battle)
        {
            Battle = battle;
            UserName = userName;
            Date = DateTime.Now;
            var icon = TextImage.GetUserImageCode(userName);
            Text = TextColor.Text + "[" + TextColor.Date + Date.ToShortTimeString() + TextColor.Text + "] " + icon + " " + TextColor.Emote + UserName +
                   " has joined " + battle.Title + " (" + battle.ModName.Trim() + ").";
        }

        public string Text { get; private set; }
    }
}