using System;
using System.Collections.Generic;
using System.Linq;
using PlasmaShared;
using ZeroKLobby;

namespace ZeroKLobby.MicroLobby
{
    class FriendManager
    {
        public IEnumerable<string> Friends { get { return Enumerable.Cast<string>(Program.Conf.Friends); } }

        public event EventHandler<EventArgs<string>> FriendAdded = delegate { };
        public event EventHandler<EventArgs<string>> FriendRemoved = delegate { };

        public FriendManager()
        {
            LoadFriends();
        }


        public void AddFriend(string userName)
        {
            if (!Friends.Contains(userName)) Program.Conf.Friends.Add(userName);
            SaveFriends();
            FriendAdded(this, new EventArgs<string>(userName));
        }

        public void RemoveFriend(string userName)
        {
            Program.Conf.Friends.Remove(userName);
            SaveFriends();
            FriendRemoved(this, new EventArgs<string>(userName));
        }

        void LoadFriends() {}

        void SaveFriends()
        {
            Program.SaveConfig();
        }
    }
}