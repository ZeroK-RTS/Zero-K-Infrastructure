using System;

namespace Ratings
{
    /// <summary>
    /// The pure part of <see cref="RatingSystems"/>: day-number conversion, with no
    /// dependency on Entity Framework or the rest of the rating pipeline. Kept in its
    /// own file so it can be linked into the .NET 9 test project - see
    /// Tests.Portable/Tests.Portable.csproj.
    /// </summary>
    public partial class RatingSystems
    {
        public static int ConvertDateToDays(DateTime date)
        {
            return (int)(date.ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalDays / 1);
        }

        public static DateTime ConvertDaysToDate(int days)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(days);
        }
    }
}
