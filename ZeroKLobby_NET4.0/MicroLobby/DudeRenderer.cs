using System.Drawing;

namespace ZeroKLobby.MicroLobby
{
    public static class DudeRenderer
    {
        public static Bitmap GetImage(int playerCount,
                                      int friendCount,
                                      int adminCount,
                                      int botCount,
                                      int maxPlayerCount,
                                      bool myBattle,
                                      int sizeX,
                                      int sizeY)
        {
            var image = new Bitmap(sizeX, sizeY);
            try
            {
                using (var g = Graphics.FromImage(image))
                {
                    var x = 0;
                    var y = 0;

                    if (myBattle)
                    {
                        g.DrawImage(ZklResources.jimi, x, y, ZklResources.jimi.Width, ZklResources.jimi.Height);
                        x += ZklResources.jimi.Width;
                    }

                    for (var i = 0; i < friendCount; i++)
                    {
                        if (x + ZklResources.friend.Width > sizeX)
                        {
                            x = 0;
                            y += ZklResources.friend.Height;
                        }
                        g.DrawImage(ZklResources.friend, x, y, ZklResources.friend.Width, ZklResources.friend.Height);
                        x += ZklResources.friend.Width;
                    }
                    for (var i = 0; i < adminCount; i++)
                    {
                        if (x + ZklResources.police.Width > sizeX)
                        {
                            x = 0;
                            y += ZklResources.police.Height;
                        }
                        g.DrawImage(ZklResources.police, x, y, ZklResources.police.Width, ZklResources.police.Height);
                        x += ZklResources.friend.Width;
                    }
                    for (var i = 0; i < playerCount; i++)
                    {
                        if (x + ZklResources.user.Width > sizeX)
                        {
                            x = 0;
                            y += ZklResources.user.Height;
                        }
                        g.DrawImage(ZklResources.user, x, y, ZklResources.user.Width, ZklResources.user.Height);
                        x += ZklResources.user.Width;
                    }
                    for (var i = 0; i < maxPlayerCount - playerCount - friendCount; i++)
                    {
                        if (x + ZklResources.user.Width > sizeX)
                        {
                            x = 0;
                            y += ZklResources.user.Height;
                        }
                        g.DrawImage(ZklResources.grayuser, x, y, ZklResources.user.Width, ZklResources.user.Height);
                        x += ZklResources.user.Width;
                    }
                }
            } 
            catch
            {
                image.Dispose();
                throw;
            }
            return image;
        }
    }
}