using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;

namespace ZkData
{
    public class Avatar
    {
        [Key]
        [StringLength(50)]
        public string AvatarName { get; set; }

        private static List<Avatar> cachedList = null;
        public static List<Avatar> GetCachedList(ZkDataContext db = null)
        {

            try
            {
                if (cachedList == null)
                {
                    cachedList = (db ?? new ZkDataContext()).Avatars.ToList();
                }
                return cachedList;
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Failed getting avatars: {0}", ex);
                return new List<Avatar>();
            }
        }
    }
}
