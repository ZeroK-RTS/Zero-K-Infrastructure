using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CMissionLib
{
	public interface ITimeSpan
	{
        int Frames { get; set; }
		double Seconds { get; set; }

        //void RaiseTimeChanged();
	}
}
