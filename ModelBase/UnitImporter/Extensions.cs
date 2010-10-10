using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace UnitImporter
{
    public static class Extensions
    {
        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (var element in source) {
                action(element);
            }
        }

        public static string GetString(this XContainer container, string name)
        {
            var element = container.Element(name);
            return element != null ? element.Value : null;
        }
    }
}