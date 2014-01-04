using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace PlasmaShared
{
    public class Whois
    {
        const string whoisServer = "whois.ripe.net";

        public Dictionary<string, string> QueryByIp(string ip) {
            var data = QueryWhois("-s ripe-grs,radb-grs,lacnic-grs,jpirr-grs,arin-grs,apnic-grs,afrinic-grs -l " + ip);
            var result = new Dictionary<string, string>();
            foreach (var line in data.Split('\n').Where(x=>!string.IsNullOrEmpty(x) && x[0] != '%')) {
                var pieces = line.Split(new char[]{':'}, 2);
                result[pieces.First().Trim()] = pieces.Last().Trim();
            }
            return result;
        }

        public string QueryWhois(string command) {
            var tcp = new TcpClient(whoisServer, 43);
            var stream = tcp.GetStream();
            var streamWriter = new StreamWriter(stream);
            streamWriter.WriteLine(command);
            streamWriter.Flush();
            return new StreamReader(stream).ReadToEnd();
        }
    }
}
