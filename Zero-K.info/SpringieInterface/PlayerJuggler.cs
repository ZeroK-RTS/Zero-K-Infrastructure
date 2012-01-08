using System.Collections.Generic;

namespace ZeroKWeb.SpringieInterface
{
    public class JugglerAutohost
    {
        public BattleContext LobbyContext;
        public BattleContext RunningGameStartContext;
    }

    public class PlayerJuggler
    {
        public static JugglerResult JugglePlayers(List<JugglerAutohost> autohosts)
        {
            var ret = new JugglerResult();

            return ret;
        }
    }

    public class JugglerMove
    {
        public string Name;
        public string TargetAutohost;
    }

    public class JugglerResult
    {
        public List<string> AutohostsToClose;
        public string Message;
        public List<JugglerMove> PlayerMoves;
    }
}