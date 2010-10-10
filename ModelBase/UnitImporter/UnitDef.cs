using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace UnitImporter
{
    public class UnitDef
    {
        public UnitDef(XElement unit)
        {
            var buildOptions = unit.Element("buildoptions");
            BuildOptionNames = buildOptions != null ? buildOptions.Descendants().Select(d => d.Value).ToArray() : (new string[0]);
            Name = XmlConvert.DecodeName(unit.Name.LocalName);
            FullName = unit.GetString("name");
            Description = unit.GetString("description");
        }

        public string[] BuildOptionNames { get; set; }
        public UnitDef[] BuildOptions { get; set; }
        public string Name { get; set; }
        public string FullName { get; set; }
        public string Description { get; set; }
        public bool IsInBuildTree { get; set; }
        public UnitDef Parent { get; set; }
    }
}