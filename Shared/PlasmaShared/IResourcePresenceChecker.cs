using System;
using System.Collections.Generic;
using System.Linq;

namespace ZkData
{
    public interface IResourcePresenceChecker: IDisposable
    {
        bool HasResource(string internalName);
    }
}