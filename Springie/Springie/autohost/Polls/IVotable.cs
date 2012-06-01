using LobbyClient;

namespace Springie.autohost.Polls
{
    public interface IVotable
    {
        bool Setup(TasSayEventArgs e, string[] words);
        void End();
        bool Vote(TasSayEventArgs e, bool vote);
    }
}