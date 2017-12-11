using System;
using System.Collections.Generic;
using ZkData;

namespace Ratings
{
    public interface IRatingUpdateListener
    {

        void RatingUpdated(IEnumerable<Account> players, RatingCategory ratingCategory);
    }
}
