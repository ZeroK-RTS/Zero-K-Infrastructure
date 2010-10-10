using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace UnitImporter
{
    public class Mod
    {
        public Mod(string xmlFile)
        {
            var document = XDocument.Parse(File.ReadAllText(xmlFile));
            var mod = document.Element("mod");
            var revisionAttribute = mod.Attribute("revision");
            Revision = revisionAttribute == null ? null : revisionAttribute.Value;
            Units = document.Descendants("unitdefs").Elements().Select(u => new UnitDef(u)).ToArray();

            // now that we loaded all the units, we can set the build option references
            Units.ForEach(u => u.BuildOptions = u.BuildOptionNames.Select(n => Units.First(nu => nu.Name == n)).ToArray());

            // find what's in the build tree and set the unit parents
            MarkBuildTree(Units.Single(u => u.Name == "armcom"));
            MarkBuildTree(Units.Single(u => u.Name == "corcom"));
        }

        const string TempXmlFile = "defs.lua";

        public Mod GetLatestCA(string springPath)
        {
            var exporter = new ModToXml.ModExporter(springPath);
            exporter.DumpLatestCA(TempXmlFile);
            return new Mod(TempXmlFile);
        }

        static public Mod FromPath(string springPath, string archivePath)
        {
            var exporter = new ModToXml.ModExporter(springPath);
            exporter.DumpModFromPath(TempXmlFile, archivePath);
            return new Mod(TempXmlFile);
        }

        static public Mod FromModName(string springPath, string modName)
        {
            var exporter = new ModToXml.ModExporter(springPath);
            exporter.DumpModFromName(TempXmlFile, modName);
            return new Mod(TempXmlFile);

        }

        public string Revision { get; set; }
        public ICollection<UnitDef> Units { get; set; }

        static void MarkBuildTree(UnitDef builder)
        {
            builder.IsInBuildTree = true;
            foreach (var unit in builder.BuildOptions) {
                if (!unit.IsInBuildTree) {
                    unit.Parent = builder;
                    MarkBuildTree(unit);
                }
            }
        }
    }
}