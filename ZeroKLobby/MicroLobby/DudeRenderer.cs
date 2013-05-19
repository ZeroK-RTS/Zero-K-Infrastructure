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
											g.DrawImage(Resources.jimi, x, y, Resources.jimi.Width, Resources.jimi.Height);
											x += Resources.jimi.Width;
                    }

                    for (var i = 0; i < friendCount; i++)
                    {
                        if (x + Resources.friend.Width > sizeX)
                        {
                            x = 0;
                            y += Resources.friend.Height;
                        }
                        g.DrawImage(Resources.friend, x, y, Resources.friend.Width, Resources.friend.Height);
                        x += Resources.friend.Width;
                    }
                    for (var i = 0; i < adminCount; i++)
                    {
                        if (x + Resources.police.Width > sizeX)
                        {
                            x = 0;
                            y += Resources.police.Height;
                        }
                        g.DrawImage(Resources.police, x, y, Resources.police.Width, Resources.police.Height);
                        x += Resources.friend.Width;
                    }
                    for (var i = 0; i < playerCount; i++)
                    {
                        if (x + Resources.user.Width > sizeX)
                        {
                            x = 0;
                            y += Resources.user.Height;
                        }
                        g.DrawImage(Resources.user, x, y, Resources.user.Width, Resources.user.Height);
                        x += Resources.user.Width;
                    }
                    for (var i = 0; i < maxPlayerCount - playerCount - friendCount; i++)
                    {
                        if (x + Resources.user.Width > sizeX)
                        {
                            x = 0;
                            y += Resources.user.Height;
                        }
                        g.DrawImage(Resources.grayuser, x, y, Resources.user.Width, Resources.user.Height);
                        x += Resources.user.Width;
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