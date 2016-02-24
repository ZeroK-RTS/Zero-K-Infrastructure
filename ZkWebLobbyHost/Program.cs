using System;
using System.IO;
using System.Windows.Forms;

namespace ZkWebLobbyHost
{
    internal static class Program
    {
        //[STAThread]
        private static void Main(params string[] args) {
            new ZkWebLobbyHost().Run(Path.GetDirectoryName(Path.GetFullPath(Application.ExecutablePath)), args);
        }
    }
}