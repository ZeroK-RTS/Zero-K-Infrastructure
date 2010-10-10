#region using

using System;
using System.Collections.Generic;
using System.Linq;

#endregion

namespace ModStats
{
	public static class LinqExtensions
	{
		#region Public methods

		public static double Median(this IEnumerable<double> source)
		{
			if (source.Count() == 0) throw new InvalidOperationException("Cannot compute median for an empty set.");

			IOrderedEnumerable<double> sortedList = from number in source orderby number select number;

			int itemIndex = sortedList.Count()/2;

			if (sortedList.Count()%2 == 0) {
				// Even number of items.
				return (sortedList.ElementAt(itemIndex) + sortedList.ElementAt(itemIndex - 1))/2;
			} else {
				// Odd number of items.
				return sortedList.ElementAt(itemIndex);
			}
		}

		/*public static Nullable<decimal> Average<TSource>(
    this IEnumerable<TSource> source,
    Func<TSource, Nullable<decimal>> selector*/



		#endregion
	}
}