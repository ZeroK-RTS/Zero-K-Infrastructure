using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using LobbyClient;
using ZeroKLobby;

namespace ZeroKLobby.MicroLobby
{
    static class TextImage
    {
        const int friend = 1;
        public static readonly string Friend = TextColor.EmotChar + friend.ToString("000"); //convert "1" to "001" 
        const int self = 4;
        public static readonly string Self = TextColor.EmotChar + self.ToString("000");
        const int napoleon = 7;
        public static readonly string Napoleon = TextColor.EmotChar + napoleon.ToString("000");
        const int admin = 2;
        public static readonly string Police = TextColor.EmotChar + admin.ToString("000");
        const int bot = 3;
        public static readonly string Robot = TextColor.EmotChar + bot.ToString("000");
        const int smurf = 5;
        public static readonly string Smurf = TextColor.EmotChar + smurf.ToString("000");
        const int soldier = 6;
        public static readonly string Soldier = TextColor.EmotChar + soldier.ToString("000");
        const int user = 0;
        public static readonly string User = TextColor.EmotChar + user.ToString("000");
        const int grayuser = 8;
        public static readonly string GrayUser = TextColor.EmotChar + user.ToString("000");
        static Image[] bitmaps;


        static TextImage()
        {
            bitmaps = new Image[9];
            bitmaps[user] = ZklResources.user;
            bitmaps[friend] = ZklResources.friend;
            bitmaps[admin] = ZklResources.police;
            bitmaps[bot] = ZklResources.robot;
            bitmaps[smurf] = ZklResources.smurf;
            bitmaps[soldier] = ZklResources.soldier;
            bitmaps[napoleon] = ZklResources.napoleon;
            bitmaps[grayuser] = ZklResources.grayuser;



            var bm = new Bitmap(ZklResources.jimi.Width, ZklResources.jimi.Height);
            using (var gr = Graphics.FromImage(bm))
            {
                gr.DrawImage(ZklResources.jimi, new Point(0, 0));
                var myUser = Program.TasClient?.MyUser;
                if (myUser != null)
                {
                    gr.DrawImage(GetUserImageRaw(myUser), new Point(0, 0));
                }
            }
            bitmaps[self] = bm;
        }

        public static Image GetImage(int imageNumber)
        {
            if (imageNumber >= bitmaps.Length)
            {
                Trace.WriteLine("Image number out of bounds (" + imageNumber + ").");
                return ZklResources.user;
            }
            return bitmaps[imageNumber];
        }

        public static Image GetUserImage(string userName)
        {
            User user;
            if (Program.TasClient.ExistingUsers.TryGetValue(userName, out user)) {
                if (userName == Program.TasClient.UserName) return bitmaps[self];
                if (user.IsBot) return ZklResources.robot;
                if (Program.FriendManager.Friends.Contains(user.Name)) return ZklResources.friend;
                if (user.IsAdmin) return ZklResources.police;
                if (user.EffectiveElo >= 1800) return ZklResources.napoleon;
                if (user.EffectiveElo >= 1600) return ZklResources.soldier;
                if (user.EffectiveElo < 1400) return ZklResources.smurf;

            }
            else return ZklResources.grayuser;
            return ZklResources.user;
        }

        private static Image GetUserImageRaw(User user) {
            if (user != null)
            {
                if (user.IsBot) return ZklResources.robot;
                if (Program.FriendManager.Friends.Contains(user.Name)) return ZklResources.friend;
                if (user.IsAdmin) return ZklResources.police;
                if (user.EffectiveElo >= 1800) return ZklResources.napoleon;
                if (user.EffectiveElo >= 1600) return ZklResources.soldier;
                if (user.EffectiveElo < 1400) return ZklResources.smurf;
            }
            return ZklResources.user;
        }

        public static string GetUserImageCode(string userName)
        {
            User user;
            if (Program.TasClient.ExistingUsers.TryGetValue(userName, out user))
            {
                if (userName == Program.TasClient.UserName) return Self;
                if (user.IsBot) return Robot;
                if (Program.FriendManager.Friends.Contains(user.Name)) return Friend;
                if (user.IsAdmin) return Police;
                if (user.EffectiveElo > 1800) return Napoleon;
                if (user.EffectiveElo > 1600) return Soldier;
                if (user.EffectiveElo < 1400) return Smurf;
                return User;
            }
            else return GrayUser;
            //return String.Empty;
        }
    }
}