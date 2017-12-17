using System;
using System.Collections.Generic;
using ZkData;

namespace Ratings
{
    public interface ITopPlayersUpdateListener
    {
        void TopPlayersUpdated(IEnumerable<Account> players);
    }
}
