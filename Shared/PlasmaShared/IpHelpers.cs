using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace PlasmaShared
{
    public static class IpHelpers
    {
        private static bool CheckMask(IPAddress address, IPAddress mask, IPAddress target)
        {
            if (mask == null) return false;

            var ba = address.GetAddressBytes();
            var bm = mask.GetAddressBytes();
            var bb = target.GetAddressBytes();

            if ((ba.Length != bm.Length) || (bm.Length != bb.Length)) return false;

            for (var i = 0; i < ba.Length; i++)
            {
                int m = bm[i];

                var a = ba[i] & m;
                var b = bb[i] & m;

                if (a != b) return false;
            }

            return true;
        }

        public static bool IsLanIP(string ip)
        {
            var address = IPAddress.Parse(ip);
            var interfaces = NetworkInterface.GetAllNetworkInterfaces();
            foreach (var iface in interfaces)
            {
                var properties = iface.GetIPProperties();
                foreach (var ifAddr in properties.UnicastAddresses)
                    if ((ifAddr.IPv4Mask != null) && (ifAddr.Address.AddressFamily == AddressFamily.InterNetwork) &&
                        CheckMask(ifAddr.Address, ifAddr.IPv4Mask, address)) return true;
            }
            return false;
        }

        public static string GetMyIpAddress()
        {
            var options = Dns.GetHostEntry(Dns.GetHostName()).AddressList.ToList();
            var interfaces = NetworkInterface.GetAllNetworkInterfaces();
            foreach (var iface in interfaces)
            {
                var properties = iface.GetIPProperties();
                foreach (var ifAddr in properties.UnicastAddresses)
                    if (ifAddr.Address.AddressFamily == AddressFamily.InterNetwork) options.Add(ifAddr.Address);
            }
            
            return options.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork && !IsLanIP(ip.ToString()))?.ToString() ?? "127.0.0.1";
        }

    }
}