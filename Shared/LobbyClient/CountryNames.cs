using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LobbyClient
{
    public static class CountryNames
    {
        public static readonly Dictionary<string, string> Names = new Dictionary<string, string>();

        static CountryNames()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            //Stream countryNames = assembly.GetManifestResourceStream(typeof(User), "Resources.CountryNames.txt"); //Fixme: GetManifestResourceStream with only 1 argument do not work in MonoDevelop
            Stream countryNames = assembly.GetManifestResourceStream("LobbyClient.Resources.CountryNames.txt");
            var reader = new StreamReader(countryNames);

            string line;
            while ((line = reader.ReadLine()) != null)
            {
                string[] parts = line.Split('\t');
                Names.Add(parts[0], parts[1]);
            }
        }

        public static string GetName(string code)
        {
            string name;
            if (CountryNames.Names.TryGetValue(code, out name)) return name;
            return "unknown";
        }
    }
}
