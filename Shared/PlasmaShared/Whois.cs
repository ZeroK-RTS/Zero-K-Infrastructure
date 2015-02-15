using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ZkData
{
    public class Whois
    {
        const string whoisServer = "whois.ripe.net";
        const string sourcesGRS = "ripe-grs,radb-grs,lacnic-grs,jpirr-grs,arin-grs,apnic-grs,afrinic-grs";
        const string sourcesNoGRS = "ripe";

        public Dictionary<string, string> QueryByIp(string ip, bool useGRS = false) {
            string sources = "-s " + (useGRS ? sourcesGRS : sourcesNoGRS );
            var data = QueryWhois(sources + " -l " + ip);
            var result = new Dictionary<string, string>();
            foreach (var line in data.Split('\n').Where(x=>!string.IsNullOrEmpty(x) && x[0] != '%')) {
                var pieces = line.Split(new char[]{':'}, 2);
                var key = pieces.First().Trim();
                var value = pieces.Last().Trim();
                if (!result.ContainsKey(key)) result[key] = value;
            }
            return result;
        }

        public string QueryWhois(string command) {
            var tcp = new TcpClient();
            tcp.Connect(whoisServer, 43);
            var stream = tcp.GetStream();

            var streamWriter = new StreamWriter(stream);
            streamWriter.WriteLine(command);
            streamWriter.Flush();
            return new StreamReader(stream).ReadToEnd();
        }
    }
}
