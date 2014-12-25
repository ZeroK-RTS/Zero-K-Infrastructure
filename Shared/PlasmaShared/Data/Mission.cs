using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace ZkData
{
    partial class Mission
    {
        [Obsolete("Do not use just for serializaiton hack")]
        string authorName;

        [Obsolete("Do not ever write to this!")]
        [DataMember]
        public string AuthorName
        {
            get
            {
                if (authorName != null) return authorName;
                else return Account != null ? Account.Name : null;
            }
            set { authorName = value; }
        }
        public string MinToMaxHumansString
        {
            get
            {
                if (MinHumans != MaxHumans) return string.Format("{0} to {1}", MinHumans, MaxHumans);
                else return MinHumans.ToString();
            }
        }

        public string NameWithVersion
        {
            get
            {
                if (IsScriptMission) return Name;
                else return string.Format("{0} r{1}", Name, Revision);
            }
        }

        public string SanitizedFileName
        {
            get
            {
                var fileName = NameWithVersion;
                foreach (var character in Path.GetInvalidFileNameChars()) fileName = fileName.Replace(character, '_');
                return fileName + ".sdz";
            }
        }

        public List<string> GetPseudoTags()
        {
            var tags = new List<string>();
            if (MinHumans < 2) tags.Add("Singleplayer");
            if (MaxHumans > 1 && IsCoop) tags.Add("Coop");
            if (MaxHumans > 1 && !IsCoop) tags.Add("Adversarial");
            return tags;
        }


        public static string GetNameWithoutVersion(string missionName)
        {
            return Regex.Replace(missionName, " r[0-9]+", "");
        }
    }
}