using System.Collections.Generic;

namespace PlasmaShared
{
    public class ScriptMissionData: ApiResponse
    {
        public List<string> ManualDependencies;
        public string MapName;
        public string ModName;
        public string Name;
        public string StartScript;
    }
}