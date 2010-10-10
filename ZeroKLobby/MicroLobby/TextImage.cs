using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using LobbyClient;
using SpringDownloader;

namespace SpringDownloader.MicroLobby
{
    static class TextImage
    {
        const int admin = 2;
        const int bot = 3;
        const int friend = 1;
        public static readonly string Friend = TextColor.EmotChar + friend.ToString("000");
        const int jimi = 4;
        public static readonly string Jimi = TextColor.EmotChar + jimi.ToString("000");
        const int napoleon = 7;
        public static readonly string Napoleon = TextColor.EmotChar + napoleon.ToString("000");

        public static readonly string Police = TextColor.EmotChar + admin.ToString("000");
        public static readonly string Robot = TextColor.EmotChar + bot.ToString("000");
        const int smurf = 5;
        public static readonly string Smurf = TextColor.EmotChar + smurf.ToString("000");
        const int soldier = 6;
        public static readonly string Soldier = TextColor.EmotChar + soldier.ToString("000");
        const int user = 0;
        public static readonly string User = TextColor.EmotChar + user.ToString("000");
        static Image[] bitmaps;


        static TextImage()
        {
            bitmaps = new Image[8];
						bitmaps[user] = Resources.user;
						bitmaps[friend] = Resources.Friend;
						bitmaps[admin] = Resources.police;
						bitmaps[bot] = Resources.robot;
						bitmaps[jimi] = Resources.jimi;
						bitmaps[smurf] = Resources.smurf;
						bitmaps[soldier] = Resources.soldier;
						bitmaps[napoleon] = Resources.napoleon;
        }

        public static Image GetImage(int imageNumber)
        {
            if (imageNumber >= bitmaps.Length)
            {
                Trace.WriteLine("Image numbe out of bounds (" + imageNumber + ").");
								return Resources.user;
            }
            return bitmaps[imageNumber];
        }

        public static Image GetUserImage(string userName)
        {
            User user;
            if (Program.TasClient.ExistingUsers.TryGetValue(userName, out user))
            {
							if (userName == Program.TasClient.UserName) return Resources.jimi;
							if (user.IsBot) return Resources.robot;
							if (Program.FriendManager.Friends.Contains(user.Name)) return Resources.Friend;
							if (user.IsAdmin) return Resources.police;
							if (user.Rank > 5) return Resources.napoleon;
							if (user.Rank > 3) return Resources.soldier;
							if (user.Rank == 0) return Resources.smurf;
            }
						return Resources.user;
        }

        public static string GetUserImageCode(string userName)
        {
            User user;
            if (Program.TasClient.ExistingUsers.TryGetValue(userName, out user))
            {
                if (userName == Program.TasClient.UserName) return Jimi;
                if (user.IsBot) return Robot;
                if (Program.FriendManager.Friends.Contains(user.Name)) return Friend;
                if (user.IsAdmin) return Police;
                if (user.Rank > 5) return Napoleon;
                if (user.Rank > 3) return Soldier;
                if (user.Rank == 0) return Smurf;
                return User;
            }
            return String.Empty;
        }
    }
}