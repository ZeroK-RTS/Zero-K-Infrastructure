﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LobbyClient;
using ZeroKWeb.SpringieInterface;
using ZkData;

namespace ZkLobbyServer
{
    public class CmdListMaps : BattleCommand
    {
        private List<Resource> maps;
        public override string Help => "[<filters>..] - lists maps on server, e.g. !listmaps altor div";
        public override string Shortcut => "listmaps";
        public override AccessType Access => AccessType.NoCheck;

        public override BattleCommand Create() => new CmdListMaps();

        public override string Arm(ServerBattle battle, Say e, string arguments = null)
        {
            maps = MapPicker.FindResources(ResourceType.Map, arguments, battle.MinimalMapSupportLevel).Take(200).ToList();
            return String.Empty;
        }


        public override async Task ExecuteArmed(ServerBattle battle, Say e)
        {
            if (maps.Any())
            {
                await battle.Respond(e, "---");
                foreach (var map in maps) await battle.Respond(e, map.ResourceID + ": " + map.InternalName);
                await battle.Respond(e, "---");
            }
            else await battle.Respond(e, "no such map found");
        }
    }
}