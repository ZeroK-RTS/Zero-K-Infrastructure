using System;
using System.Collections.Generic;

namespace PlasmaShared
{
    /// <summary>
    /// Methods the .NET Framework does not have and .NET 9 does.
    ///
    /// **This file exists to be EXCLUDED.** ZkLobbyServer.Core leaves it out of its compile list,
    /// so on .NET 9 the framework's own implementation is used and there is nothing to disambiguate;
    /// the Framework projects compile it and get the behaviour they always had.
    ///
    /// It was split out of Utils.cs because the two DistinctBy overloads are equally applicable
    /// extension methods once both exist, which is an ambiguity error rather than one quietly
    /// winning - found by compiling the lobby-server chain as one assembly for the first time.
    ///
    /// Same semantics as System.Linq's, which is what makes the swap safe: first element per key,
    /// source order preserved.
    /// </summary>
    public static partial class Utils
    {
        public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            //from https://stackoverflow.com/questions/489258/linqs-distinct-on-a-particular-property
            HashSet<TKey> seenKeys = new HashSet<TKey>();
            foreach (TSource element in source)
            {
                if (seenKeys.Add(keySelector(element)))
                {
                    yield return element;
                }
            }
        }
    }
}
