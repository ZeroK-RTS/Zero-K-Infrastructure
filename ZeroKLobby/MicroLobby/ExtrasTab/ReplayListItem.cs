using System;
using System.Drawing;
using System.Linq;
using LobbyClient;
using ZkData.UnitSyncLib;
using System.Collections.Generic;

namespace ZeroKLobby.MicroLobby.ExtrasTab
{
    public class ReplayListItem: IDisposable
    {
        int height = 16;
        
        public string fileName;
        public string filePath;
        public string balance;
        public int aiCount;
        public List<string> players = new List<string>();
        public List<string> ais= new List<string>();
        public int averageRank;
        public int averageElo;
        public string engine;
        public string mapName;
        public DateTime dateTime;
        public int replaySize;
        public string gameName;
        public string hostName;
        public bool haveBeenUpdated;

        public int Height { get { return height; } set { height = value; } }


        public ReplayListItem()
        {
        }

        ~ReplayListItem()
        {
            Dispose();
            GC.SuppressFinalize(this);
        }


        public void Dispose()
        {
            
        }

        public override string ToString()
        {
            return fileName;
        }
    }
}