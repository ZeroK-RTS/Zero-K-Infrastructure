using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CMissionLib
{
    /// <summary>
    /// Implemented by (most) conditions and actions that specify a period of time
    /// </summary>
	public interface ITimeSpan
	{
        int Frames { get; set; }
		double Seconds { get; set; }

        //void RaiseTimeChanged();
	}
}
