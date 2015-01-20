#region using

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.AccessControl;
using System.Threading;

#endregion

namespace PlasmaDownloader
{
    public abstract class Download
    {
        readonly object finishLock = new object();
        bool? individualComplete;
        Double lastProgress = 0;
        DateTime lastSpeedAsk = DateTime.Now;
        protected List<Download> neededDownloads = new List<Download>();
        readonly List<Download> parents = new List<Download>();
        public DownloadType TypeOfResource { get; internal set; }

        public int AverageSpeed { get { return (int)((TotalProgress/100.0*TotalLength)/DateTime.Now.Subtract(Started).TotalSeconds); } }
        public int CurrentSpeed
        {
            get
            {
                var ret = AverageSpeed;
                var now = DateTime.Now;
                var tp = TotalProgress;
                if (lastProgress > 0 && now.Subtract(lastSpeedAsk).TotalSeconds > 1) ret = (int)((tp - lastProgress)/100.0*TotalLength/now.Subtract(lastSpeedAsk).TotalSeconds);
                lastProgress = tp;
                lastSpeedAsk = now;
                return ret;
            }
        }


        public virtual double IndividualProgress { get; protected internal set; }

        public bool IsAborted { get; protected set; }


        /// <summary>
        /// True  - download ok, false - download failed, null - still downloading
        /// </summary>
        public bool? IsComplete { get; protected set; }

        public int Length { get; protected internal set; }
        public string Name { get; protected set; }

        public virtual IEnumerable<Download> NeededDownloads { get { return neededDownloads; } }

        public int SecondsRemaining
        {
            get
            {
                var totProg = TotalProgress;
                if (totProg <= 0) return -1;
                return (int)(DateTime.Now.Subtract(Started).TotalSeconds/totProg*(100.0 - totProg));
            }
        }

        public DateTime Started { get; protected set; }

        public string TimeRemaining
        {
            get
            {
                var seconds = SecondsRemaining;
                if (seconds > 3600) return "?";
                else return string.Format("{0}:{1:00}", seconds/60, seconds%60);
            }
        }
        public int TotalLength { get { return Length + neededDownloads.Sum(x => x.TotalLength); } }

        public double TotalProgress
        {
            get
            {
                if (Length > 0 && !NeededDownloads.Any(x => x.Length == 0))
                {
                    // lengths are known, calculate exact progress
                    var totalLength = Length;
                    var doneLength = (IndividualProgress/100.0*Length);
                    foreach (var d in NeededDownloads)
                    {
                        doneLength += (d.TotalProgress/100.0*d.TotalLength);
                        totalLength += d.TotalLength;
                    }
                    return (doneLength*100.0/totalLength);
                }
                else
                {
                    // lengths are not known, weight each download as equal
                    var cnt = 1;
                    var total = IndividualProgress;
                    foreach (var d in NeededDownloads)
                    {
                        total += d.TotalProgress;
                        cnt++;
                    }
                    return total/cnt;
                }
            }
        }

        public EventWaitHandle WaitHandle { get; protected set; }

        protected Download()
        {
            WaitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);
            Started = DateTime.Now;
        }

        public virtual void Abort()
        {
            IsAborted = true;
        }

        protected internal void AddNeededDownload(Download down)
        {
					if (down != this)
					{
						down.parents.Add(this);
						neededDownloads.Add(down);
					} else Trace.TraceWarning(string.Format("{0} depends on itself", down.Name));
        }

        protected internal virtual void Finish(bool isComplete)
        {
            lock (finishLock)
            {
                individualComplete = isComplete;
                if (isComplete) IndividualProgress = 100;

                // todo fix threading this can sometimes fail due to threads
                if (!neededDownloads.Any(x => x.IsComplete == null))
                {
                    // no remaining dependencies
                    if (isComplete) IsComplete = !neededDownloads.Any(x => x.IsComplete == false); // set as complete only if all dependencies are ok
                    else IsComplete = false;
                    WaitHandle.Set();
                }

                foreach (var parent in parents)
                {
                    if (parent != null && parent.individualComplete != null)
                    {
                        // propagate completion up to parent
                        parent.Finish(parent.individualComplete.Value);
                    }
                }
            }
        }
    }
}