using System.Collections.Generic;
using ZkData;

namespace ZkLobbyServer
{
    public interface ITopPlayerProvider {
        List<Account> GetTop50();
        List<Account> GetTop50Casual();
    }

}