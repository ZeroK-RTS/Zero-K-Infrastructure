using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ZkData
{
    partial class Avatar
    {
        private static List<Avatar> cachedList = null;
        public static List<Avatar> GetCachedList() {
            if (cachedList == null) cachedList = new ZkDataContext().Avatars.ToList();
            return cachedList;
        }
    }
}
