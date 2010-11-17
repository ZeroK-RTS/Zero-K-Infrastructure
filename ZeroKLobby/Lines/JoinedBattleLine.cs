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
            Text = string.Format("{0}[{1}{2}{3}] {4} {5}{6} has joined {7} ({8}) - spring://@join_battle:{9} ", TextColor.Text, TextColor.Date, Date.ToShortTimeString(), TextColor.Text, icon, TextColor.Emote, UserName, battle.Title, battle.ModName.Trim(), battle.Founder);
        }

        public string Text { get; private set; }
    }
}