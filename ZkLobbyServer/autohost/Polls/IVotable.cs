using LobbyClient;

namespace Springie.autohost.Polls
{
    public interface IVotable
    {
        bool Setup(Say e, string[] words);
        void End();
        bool Vote(Say e, bool vote);
        string Creator { get; }
    }
}