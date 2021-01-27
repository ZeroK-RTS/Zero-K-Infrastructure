using System;

namespace PlasmaShared
{
    public enum RatingCategory
    {
        Casual = 1, MatchMaking = 2, Planetwars = 4
    }
    [Flags]
    public enum RatingCategoryFlags
    {
        Casual = 1, MatchMaking = 2, Planetwars = 4
    }
    public enum RatingSearchOption
    {
        Any = 0, Casual = 1, Competitive = 2, Planetwars = 3, None = 4
    }
}
