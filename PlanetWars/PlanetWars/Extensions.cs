using System;
using System.Collections.Generic;

namespace PlanetWars
{
    public static class Extensions
    {
        public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> action)
        {
            foreach (var cur in enumerable) {
                action(cur);
            }
        }
    }
}