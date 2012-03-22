using System;
using LobbyClient;
using ZeroKLobby.MicroLobby;

namespace ZeroKLobby.Lines
{
  class SelfJoinedBattleLine: IChatLine
  {
    protected Battle Battle { get; private set; }

    public SelfJoinedBattleLine(Battle battle)
    {
      Battle = battle;
      Date = DateTime.Now;
      Text = string.Format("{0}[{1}{2}{3}] ==== YOU HAVE JOINED BATTLE ROOM: {4} - {5} ====",
                           TextColor.Join,
                           TextColor.Date,
                           Date.ToShortTimeString(),
                           TextColor.Join,
                           battle.Founder,
                           battle.Title);
    }

    public DateTime Date { get; private set; }

    public string Text { get; private set; }
  }
}