using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace ZkData
{
    public class MiscVar
    {
        private static ConcurrentDictionary<string, string> cache = new ConcurrentDictionary<string, string>();

        public static string DefaultEngine { get { return GetValue("engine") ?? GlobalConst.DefaultEngineOverride; } set { SetValue("engine", value); } }
        public static string LastRegisteredZkVersion { get { return GetValue("zkVersion"); } set { SetValue("zkVersion", value); } }
        public static string LastRegisteredChobbyVersion { get { return GetValue("chobbyVersion"); } set { SetValue("chobbyVersion", value); } }
        
        public static string ServerPubKey { get { return GetValue("serverPubKey"); } set { SetValue("serverPubKey", value); } }

        public static string ServerPrivKey { get { return GetValue("serverPrivKey"); } set { SetValue("serverPrivKey", value); } }


        public static bool IsZklsLimited => ZklsMaxUsers > 0;

        public static int ZklsMaxUsers
        {
            get
            {
                int maxp;
                int.TryParse(GetValue("ZklsMaxUsers") ?? "", out maxp);
                return maxp;
            }
            set
            {
                SetValue("ZklsMaxUsers", value.ToString());
            }
        }


        public static PlanetWarsModes PlanetWarsMode
        {
            get
            {
                PlanetWarsModes mode;
                if (Enum.TryParse(GetValue("planetWarsMode"), out mode)) return mode;
                else return PlanetWarsModes.AllOffline;
            }
            set { SetValue("planetWarsMode", value.ToString()); }
        }

        public static PlanetWarsModes? PlanetWarsNextMode
        {
            get
            {
                PlanetWarsModes mode;
                if (Enum.TryParse(GetValue("planetWarsNextMode"), out mode)) return mode;
                return null;
            }
            set { SetValue("planetWarsNextMode", value?.ToString()); }
        }


        public static DateTime? PlanetWarsNextModeTime
        {
            get
            {
                DateTime time;
                if (DateTime.TryParse(GetValue("planetWarsNextModeTime"), out time)) return time;
                return null;
            }
            set { SetValue("planetWarsNextModeTime", value?.ToString()); }
        }



        [Key]
        [StringLength(200)]
        public string VarName { get; set; }
        public string VarValue { get; set; }


        public static string GetValue(string varName)
        {
            return cache.GetOrAdd(varName,
                (vn) =>
                {
                    using (var db = new ZkDataContext())
                    {
                        return db.MiscVars.Where(x => x.VarName == varName).Select(x => x.VarValue).FirstOrDefault();
                    }
                });
        }

        public static void SetValue(string varName, string value)
        {
            cache.AddOrUpdate(varName,
                (vn) =>
                {
                    StoreDbValue(varName, value);
                    return value;
                },
                (vn, val) =>
                {
                    if (val != value) StoreDbValue(varName, value);
                    return value;
                });
        }

        private static void StoreDbValue(string varName, string value)
        {
            using (var db = new ZkDataContext())
            {
                var entry = db.MiscVars.FirstOrDefault(x => x.VarName == varName);
                if (entry == null)
                {
                    entry = new MiscVar() { VarName = varName };
                    db.MiscVars.Add(entry);
                }
                entry.VarValue = value;
                db.SaveChanges();
            }
        }
    }
}