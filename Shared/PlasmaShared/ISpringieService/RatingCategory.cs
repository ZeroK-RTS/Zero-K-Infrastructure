﻿using System;

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
}
