﻿using System;
using System.Linq;
using System.Threading.Tasks;
using LobbyClient;

namespace ZkLobbyServer
{
    public class CmdKick : BattleCommand
    {
        public override string Help => "[<filters>..] - kicks a player";
        public override string Shortcut => "kick";
        public override AccessType Access => AccessType.Anywhere;

        public override BattleCommand Create() => new CmdKick();

        private string target;

        public override string Arm(ServerBattle battle, Say e, string arguments = null)
        {
            if (string.IsNullOrEmpty(arguments))
            {
                battle.Respond(e, "You must specify a player name");
                return null;
            }

            target = battle.GetAllUserNames().FirstOrDefault(x => x.ToLower().Contains(arguments.ToLower()));
            if (target == null)
            {
                battle.Respond(e, "Player " + arguments + " not found!");
                return null;
            }
            if (target == battle.FounderName) {
                battle.Respond(e, "Cannot kick the host");
                return null;
            }

            User user = null;
            UserBattleStatus ubs = null;
            battle.Users.TryGetValue(target, out ubs);
            if (ubs != null) {
                user = ubs.LobbyUser;
            } else {
                ConnectedUser con = null;
                battle.server.ConnectedUsers.TryGetValue(target, out con);
                user = con?.User;
            }
            if (user?.IsAdmin == true) {
                battle.Respond(e, "Can't kick an admin (spec him or just ask to leave)");
                return null;
            }

            if (e != null && battle.spring.IsRunning && battle.spring.LobbyStartContext?.Players.Any(x => x.Name == e.User && !x.IsSpectator) == false)
            {
                battle.Respond(e, "Only players can invoke this during a game");
                return null;
            }
            return $"Do you want to kick {target}?";
        }


        public override async Task ExecuteArmed(ServerBattle battle, Say e)
        {
            if (battle.spring.IsRunning) battle.spring.Kick(target);
            await battle.KickFromBattle(target, $"by {e?.User}");
        }



        public override RunPermission GetRunPermissions(ServerBattle battle, string userName, out string reason)
        {
            var ret = base.GetRunPermissions(battle, userName, out reason);

            // only people from same team can vote
            if (ret == RunPermission.Vote && battle.spring.IsRunning)
            {
                var subject = battle.spring.LobbyStartContext.Players.FirstOrDefault(x => x.Name == target);
                var entry = battle.spring.LobbyStartContext.Players.FirstOrDefault(x => x.Name == userName);
                if (subject == null || subject.IsSpectator) return ret;
                if (entry != null && !entry.IsSpectator && entry.AllyID == subject.AllyID) return ret;
                return RunPermission.None;
            }
            return ret;
        }
    }
}
