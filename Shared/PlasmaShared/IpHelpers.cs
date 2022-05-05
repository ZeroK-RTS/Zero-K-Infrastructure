using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace PlasmaShared
{
    public static class IpHelpers
    {
        public const string CidrPrivateAddressBlockA = "10.0.0.0/8";
        public const string CidrPrivateAddressBlockB = "172.16.0.0/12";
        public const string CidrPrivateAddressBlockC = "192.168.0.0/16";

        static bool IsInCidrRange(string ipAddress, string cidrMask)
        {
            string[] parts = cidrMask.Split('/');

            int IP_addr = BitConverter.ToInt32(IPAddress.Parse(ipAddress).GetAddressBytes(), 0);
            int CIDR_addr = BitConverter.ToInt32(IPAddress.Parse(parts[0]).GetAddressBytes(), 0);
            int CIDR_mask = IPAddress.HostToNetworkOrder(-1 << (32 - int.Parse(parts[1])));

            return ((IP_addr & CIDR_mask) == (CIDR_addr & CIDR_mask));
        }

        /// <summary>
        /// Returns true if ip address is local address (10.x, 172.x etc)
        /// </summary>
        public static bool IsPrivateAddressSpace(string ipAddress)
        {
            var inPrivateBlockA = IsInCidrRange(ipAddress, CidrPrivateAddressBlockA);
            var inPrivateBlockB = IsInCidrRange(ipAddress, CidrPrivateAddressBlockB);
            var inPrivateBlockC = IsInCidrRange(ipAddress, CidrPrivateAddressBlockC);

            return inPrivateBlockA || inPrivateBlockB || inPrivateBlockC;
        }
        
        
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

        /// <summary>
        /// Returns whether IP belongs to LAN networks according to my interfaces IPs and masks
        /// </summary>
        public static bool IsMyLanIp(string ip)
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
            var options = new List<IPAddress>();
            var interfaces = NetworkInterface.GetAllNetworkInterfaces();
            foreach (var iface in interfaces.Where(x=>x.OperationalStatus == OperationalStatus.Up && x.NetworkInterfaceType != NetworkInterfaceType.Loopback))
            {
                var gws = iface.GetIPProperties().GatewayAddresses;
                if (gws.Count == 0) continue;
                
                var properties = iface.GetIPProperties();
                foreach (var ifAddr in properties.UnicastAddresses)
                    if (ifAddr.Address.AddressFamily == AddressFamily.InterNetwork) options.Add(ifAddr.Address);
            }
            options.AddRange(Dns.GetHostEntry(Dns.GetHostName()).AddressList);            
            
            return options.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork && !IsPrivateAddressSpace(ip.ToString()))?.ToString() ?? "127.0.0.1";
        }

    }
}