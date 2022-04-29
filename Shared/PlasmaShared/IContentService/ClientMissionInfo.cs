using System.Collections.Generic;
using System.Linq;

namespace PlasmaShared
{
    public class ClientMissionInfo: ApiResponse
    {
        public string Author { get; set; }
        public string OtherDependencies { get; set; }
        public string Description { get; set; }
        public float? Difficulty { get; set; }
        public string DisplayName { get; set; }
        public string DownloadHandle { get; set; }
        public string ImageUrl { get; set; }
        public bool IsScriptMission { get; set; }
        public string Map { get; set; }
        public int MissionID { get; set; }
        public string Mod { get; set; }
        public float? Rating { get; set; }
        public int Revision { get; set; }
        public string Script { get; set; }

        public float? FeaturedOrder { get; set; }
    }
}