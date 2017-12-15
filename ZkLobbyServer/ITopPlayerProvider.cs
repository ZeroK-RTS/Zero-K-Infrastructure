using System;
using System.Collections.Generic;
using ZkData;

namespace ZkLobbyServer
{
    public interface ITopPlayerProvider {
        List<Account> GetTop();
        List<Account> GetTopCasual();
        event EventHandler<ITopPlayerProvider> TopPlayersUpdated;
    }

}