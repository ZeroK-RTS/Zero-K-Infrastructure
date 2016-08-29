using System.Collections.Generic;
using System.Linq;

namespace ZkData
{
    public interface IPlanetwarsEventCreator
    {
        Event CreateEvent(string format, params object[] args);
        void GhostPm(string user, string text);
    }

    
}