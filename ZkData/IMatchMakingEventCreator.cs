using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZkData
{
    public interface IMatchMakingEventCreator
    {
        MMEvent CreateEvent(string format, params object[] args);
    }
}
