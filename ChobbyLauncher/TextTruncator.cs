using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace ChobbyLauncher
{
    public static class TextTruncator
    {
        private static readonly string TruncationMarker = "------- TRUNCATED -------" + Environment.NewLine;

        public struct RegionOfInterest
        {
            public int PointOfInterest;
            public int StartLimit;
            public int EndLimit;
        }

        private struct Region
        {
            public int Start;
            public int End;
            public int Length { get => End - Start; }

            public int StartLimit;
            public int EndLimit;

            public static Region Merge(Region a, Region b) =>
                new Region
                {
                    Start = Math.Min(a.Start, b.Start),
                    End = Math.Max(a.End, b.End),
                    StartLimit = Math.Min(a.StartLimit, b.StartLimit),
                    EndLimit = Math.Max(a.EndLimit, b.EndLimit),
                };
        }
        private struct TruncatedTextBuilder
        {
            private int LineAlignForwards(int pos)
            {
                if (pos == 0)
                {
                    return pos;
                }
                var nextNewline = _initialString.IndexOf('\n', pos - 1);
                return nextNewline == -1 ? _initialString.Length : nextNewline + 1;
            }
            private int LineAlignBackwards(int pos)
            {
                if (pos == 0)
                {
                    return pos;
                }
                var prevNewline = _initialString.LastIndexOf('\n', pos - 1);
                return prevNewline + 1;
            }

            private readonly string _initialString;
            private readonly int _maxLength;
            private List<Region> Regions { get; set; }

            //Should always equal:
            // Regions.Sum(r => r.Length)
            private int RegionLengthSum { get; set; }

            //Should always equal:
            // Regions.Any(r => r.Start == 0)
            private bool HasRegionAtStart { get; set; }
            //Should always equal:
            // Regions.Any(r => r.End == _initialString.Length)
            private bool HasRegionAtEnd { get; set; }

            private int LengthEstimate { get => RegionLengthSum + TruncationMarker.Length * ((Regions.Count - 1) + (HasRegionAtStart ? 0 : 1) + (HasRegionAtEnd ? 0 : 1)); }

            private bool LengthChangeImpossible(int lengthChange) => LengthEstimate > _maxLength - lengthChange;

            private int PredictLengthChangeFromTerminalTruncationMarkers(int newRegionStart, int newRegionEnd) =>
                //If a region will be at the start of _initialString, and
                //there was not previously a region at the start of _initialString;
                //the TruncationMarker at the start of the result is no longer needed;
                //so there will be a negative length change.

                //(This is also true for regions at the end of _initialString).
                TruncationMarker.Length *
                (
                    (!HasRegionAtStart && newRegionStart == 0 ? -1 : 0)
                  + (!HasRegionAtEnd && newRegionEnd == _initialString.Length ? -1 : 0)
                );

            private void UpdateLengthEstimate(int newRegionStart, int newRegionEnd, int regionLengthChange)
            {
                HasRegionAtStart = HasRegionAtStart || newRegionStart == 0;
                HasRegionAtEnd = HasRegionAtEnd || newRegionEnd == _initialString.Length;
                RegionLengthSum += regionLengthChange;
            }

            public TruncatedTextBuilder(string initialString, int maxLength, int expectedRegionCount)
            {
                _initialString = initialString;
                _maxLength = maxLength;
                Regions = new List<Region>(expectedRegionCount);
                RegionLengthSum = 0;
                HasRegionAtStart = false;
                HasRegionAtEnd = false;
            }

            public bool AddRegion(RegionOfInterest newRegion)
            {
                //Cleans region before inserting it into Regions:
                //  Does not allow Regions to contain any Region with Length=0.
                //  Does not allow Regions to contain any Region where Start/End/StartLimit/EndLimit are not aligned to line boundaries.
                //  Does not allow Regions to contain any Region where Start<StartLimit or EndLimit<End.
                var region = new Region
                {
                    Start = LineAlignBackwards(newRegion.PointOfInterest),
                    End = LineAlignForwards(newRegion.PointOfInterest),
                    StartLimit = newRegion.StartLimit,
                    EndLimit = newRegion.EndLimit,
                };

                if (region.Length == 0)
                {
                    if (region.End != _initialString.Length)
                    {
                        region.End = LineAlignForwards(region.End + 1);
                    }
                    else
                    {
                        region.Start = LineAlignBackwards(region.Start - 1);
                    }
                }

                region.StartLimit = Math.Min(region.Start, LineAlignBackwards(region.StartLimit));
                region.EndLimit = Math.Max(region.End, LineAlignForwards(region.EndLimit));

                bool mergeRegion = Regions.Count > 0 && region.Start <= Regions[Regions.Count - 1].End;
                Region finalRegion;
                int regionLengthChange;
                if (mergeRegion)
                {
                    finalRegion = Region.Merge(Regions[Regions.Count - 1], region);
                    regionLengthChange = finalRegion.Length - Regions[Regions.Count - 1].Length;
                }
                else
                {
                    finalRegion = region;
                    regionLengthChange = region.Length;
                }

                var lengthChange =
                    regionLengthChange +
                    (mergeRegion ? 0 : TruncationMarker.Length) +
                    PredictLengthChangeFromTerminalTruncationMarkers(finalRegion.Start, finalRegion.End);

                if (LengthChangeImpossible(lengthChange))
                {
                    return false;
                }

                UpdateLengthEstimate(finalRegion.Start, finalRegion.End, regionLengthChange);
                if (mergeRegion)
                {
                    Regions[Regions.Count - 1] = finalRegion;
                }
                else
                {
                    Regions.Add(finalRegion);
                }

                return true;
            }

            public bool ExpandRegions()
            {
                var expanded = false;
                var expansionLocationIndex = new int[Regions.Count << 1];
                var distancesToLimit = new int[Regions.Count << 1];
                for (var i = 0; i != Regions.Count; ++i)
                {
                    expansionLocationIndex[i << 1] = i << 1;
                    distancesToLimit[i << 1] = Regions[i].Start - Regions[i].StartLimit;

                    expansionLocationIndex[(i << 1) + 1] = (i << 1) + 1;
                    distancesToLimit[(i << 1) + 1] = Regions[i].EndLimit - Regions[i].End;
                }

                Array.Sort(distancesToLimit, expansionLocationIndex);

                var remainingAmountToExpand = _maxLength - LengthEstimate;

                for (var i = 0; i != expansionLocationIndex.Length; ++i)
                {
                    var ri = expansionLocationIndex[i] >> 1;
                    var remainingRegions = expansionLocationIndex.Length - i;
                    var amountToExpand = remainingAmountToExpand / remainingRegions;

                    int newStart;
                    int newEnd;
                    int regionLengthChange;

                    if (distancesToLimit[i] <= amountToExpand)
                    {
                        //Expand by distancesToLimit[i]
                        if ((expansionLocationIndex[i] & 1) == 0)
                        {
                            newStart = Regions[ri].StartLimit;
                            newEnd = Regions[ri].End;
                        }
                        else
                        {
                            newStart = Regions[ri].Start;
                            newEnd = Regions[ri].EndLimit;
                        }

                        remainingAmountToExpand -= distancesToLimit[i] + PredictLengthChangeFromTerminalTruncationMarkers(newStart, newEnd);
                        regionLengthChange = distancesToLimit[i];
                    }
                    else
                    {
                        //Expand by amountToExpand (snap to line boundary)
                        var oldStart = Regions[ri].Start;
                        var oldEnd = Regions[ri].End;
                        if ((expansionLocationIndex[i] & 1) == 0)
                        {
                            newStart = LineAlignForwards(oldStart - amountToExpand);
                            newEnd = oldEnd;
                            regionLengthChange = oldStart - newStart;
                        }
                        else
                        {
                            newStart = oldStart;
                            newEnd = LineAlignBackwards(oldEnd + amountToExpand);
                            regionLengthChange = newEnd - oldEnd;
                        }

                        //Reduce by amountToExpand, rather than the actual distance that was expanded, for fairness between expansion points.
                        remainingAmountToExpand -= amountToExpand;
                    }
                    UpdateLengthEstimate(newStart, newEnd, regionLengthChange);
                    Regions[ri] = new Region
                    {
                        Start = newStart,
                        End = newEnd,
                        StartLimit = Regions[ri].StartLimit,
                        EndLimit = Regions[ri].EndLimit,
                    };
                    if (regionLengthChange > 0)
                    {
                        expanded = true;
                    }
                }
                return expanded;
            }

            public void MergeRegions()
            {
                var i = 0;
                while (i + 1 < Regions.Count)
                {
                    if (Regions[i].End >= Regions[i + 1].Start)
                    {
                        var mergedRegion = Region.Merge(Regions[i], Regions[i + 1]);
                        RegionLengthSum += mergedRegion.Length - (Regions[i].Length + Regions[i + 1].Length);
                        Regions[i] = mergedRegion;
                        Regions.RemoveAt(i + 1);
                    }
                    else
                    {
                        ++i;
                    }
                }
            }

            public override string ToString()
            {
                //Always returns a string with a Length matching LengthEstimate.
                //If Regions is not merged, the locations of the inserted
                //TruncationMarkers might not be what you expect.

                if (Regions.Count == 0)
                {
                    return TruncationMarker;
                }
                var sb = new StringBuilder(LengthEstimate);
                if (!HasRegionAtStart)
                {
                    sb.Append(TruncationMarker);
                }
                var i = 0;
                {
                    var r = Regions[i++];
                    sb.Append(_initialString, r.Start, r.Length);
                }
                while (i != Regions.Count)
                {
                    var r = Regions[i++];
                    sb.Append(TruncationMarker);
                    sb.Append(_initialString, r.Start, r.Length);
                }
                if (!HasRegionAtEnd)
                {
                    sb.Append(TruncationMarker);
                }
                return sb.ToString();
            }
        }

        public static string Truncate(string str, int maxLength, IReadOnlyList<RegionOfInterest> regionsOfInterest)
        {
            //Requires:
            //  maxLength >= 0
            //  regionsOfInterest is sorted by PointOfInterest
            //  regionsOfInterest.All(r => r.StartLimit <= r.PointOfInterest && r.PointOfInterest <= r.EndLimit)
            //  regionsOfInterest.All(r => 0 <= r.PointOfInterest && r.PointOfInterest <= str.Length)
            //  regionsOfInterest.All(r => 0 <= r.EndLimit && r.EndLimit <= str.Length)
            //  regionsOfInterest.All(r => 0 <= r.StartLimit && r.StartLimit <= str.Length)

            if (maxLength < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxLength));
            }

            if (!regionsOfInterest
                    .Zip(regionsOfInterest.Skip(1), (l, r) => new { l, r })
                    .All(p => p.l.PointOfInterest <= p.r.PointOfInterest))
            {
                throw new ArgumentException("Not sorted by PointOfInterest", nameof(regionsOfInterest));
            }

            if (!regionsOfInterest.All(r => r.StartLimit <= r.PointOfInterest && r.PointOfInterest <= r.EndLimit))
            {
                throw new ArgumentException("Contains RegionOfInterest for which PointOfInterest not between StartLimit and EndLimit", nameof(regionsOfInterest));
            }

            if (!(
                regionsOfInterest.All(r => 0 <= r.PointOfInterest && r.PointOfInterest <= str.Length)
             && regionsOfInterest.All(r => 0 <= r.EndLimit && r.EndLimit <= str.Length)
             && regionsOfInterest.All(r => 0 <= r.StartLimit && r.StartLimit <= str.Length)))
            {
                throw new ArgumentException("Contains RegionOfInterest that is not within str", nameof(regionsOfInterest));
            }


            //Returns: A version of str for which Length <= maxLength
            //  To reduce the length, lines are removed and replaced with TruncationMarker
            //  When removing lines, lines that are within regionsOfInterest are preferentially kept

            if (str.Length <= maxLength)
            {
                return str;
            }

            //Special handling if maxLength < TruncationMarker.Length
            if (maxLength < TruncationMarker.Length)
            {
                return string.Empty;
            }

            var result = new TruncatedTextBuilder(str, maxLength, regionsOfInterest.Count);

            //Convert "regionsOfInterest" to regions
            //  Add them to TruncatedTextBuilder incrementally. Stop if maxLength is exceeded
            //  Snap PointOfInterest/StartLimit/EndLimit to line boundaries
            foreach (var roi in regionsOfInterest)
            {
                if (!result.AddRegion(roi))
                {
                    return result.ToString();
                }
            }
            while (result.ExpandRegions())
            {
                result.MergeRegions();
            }

            return result.ToString();
        }
    }
}
