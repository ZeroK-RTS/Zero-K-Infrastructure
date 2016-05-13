using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using LobbyClient;

namespace ZeroKLobby.MicroLobby
{
    public class BattleIcon : IDisposable, IToolTipProvider, INotifyPropertyChanged
    {
        public const int Height = 75;
        public const int Width = 300;
        const int minimapSize = 52;
        bool dirty;

        bool disposed;
        Bitmap finishedMinimap;
        Bitmap image;
        bool isInGame;


        Bitmap playersBoxImage;
        static Size playersBoxSize = new Size(214, 32);
        Image resizedMinimap;
        public static Brush BackBrush = new SolidBrush(Config.BgColor);

        public Battle Battle { get; private set; }
        public Bitmap Image
        {
            get
            {
                if (dirty)
                {
                    UpdateImage();
                    dirty = false;
                }
                return image;
            }
        }
        public bool IsInGame
        {
            get { return isInGame; }
            set
            {
                if (isInGame != value)
                {
                    isInGame = value;
                    dirty = true;
                    OnPropertyChanged("BitmapSource"); // notify wpf about icon change
                }
            }
        }
        public static Size MapCellSize = new Size(74, 70);

        public Image MinimapImage
        {
            set
            {
                if (value == null)
                {
                    resizedMinimap = null;
                    return;
                }
                resizedMinimap = new Bitmap(value, (int)minimapSize, (int)minimapSize);
                dirty = true;
                OnPropertyChanged("BitmapSource"); // notify wpf about icon change
            }
        }
        public static Font ModFont = Config.GeneralFontSmall;

        public static Brush TextBrush = new SolidBrush(Color.White); //  Config.TextColor
        public static Font TitleFont = Config.GeneralFont;
        public static Font QueueFont = Config.GeneralFont;
        public static Brush QueueBrush = new SolidBrush(Config.TextColor);
        public static Brush QueueBrushOutline = new SolidBrush(Config.BgColor);

        public BattleIcon(Battle battle)
        {
            Battle = battle;
        }

        public void Dispose()
        {
            disposed = true;
            if (resizedMinimap != null) resizedMinimap.Dispose();
            if (playersBoxImage != null) playersBoxImage.Dispose();
            if (finishedMinimap != null) finishedMinimap.Dispose();
        }

        public bool HitTest(int x, int y)
        {
            return x > 3 && x < 290 && y > 3 && y < 64 + 3;
        }

        public void SetPlayers()
        {
            dirty = true;
            OnPropertyChanged("PlayerCount");
            OnPropertyChanged("BitmapSource"); // notify wpf about icon change
        }


        void MakeMinimap()
        {
            if (resizedMinimap == null) return; // wait, map is not downloaded

            if (finishedMinimap != null) finishedMinimap.Dispose();
            finishedMinimap = new Bitmap((int)ZklResources.border.Width, (int)ZklResources.border.Height);

            using (Graphics g = Graphics.FromImage(finishedMinimap))
            {
                g.DrawImage(resizedMinimap, (int)10, (int)9);
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                int x = (int)10;
                int y = (int)(minimapSize - 20);
                Action<Image> drawIcon = image =>
                {
                    g.DrawImage(image, x, y, (int)20, (int)20);
                    x += (int)30;
                };

                if (IsInGame)
                {
                    g.DrawImage(ZklResources.boom,
                        (int)10,
                        (int)10,
                        (int)50,
                        (int)50);
                }
                if (Battle.IsOfficial() && Battle.IsSpringieManaged && !Battle.IsQueue)
                {
                    g.DrawImage(ZklResources.star,
                        (int)48,
                        (int)8,
                        (int)15,
                        (int)15);
                }
                if (Battle.IsOfficial() && Battle.IsQueue)
                {
                    g.DrawStringWithOutline(Battle.QueueName.Replace(' ', '\n'),
                        QueueFont,
                        QueueBrush,
                        QueueBrushOutline,
                        new Rectangle(4, 4, (int)62, (int)62),
                        new StringFormat() { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center},
                        3);
                }

                if (Battle.IsPassworded) drawIcon(ZklResources._lock);

                g.DrawImage(ZklResources.border, 4,4, (int)64, (int)64);
            }
        }

        Bitmap MakeSolidColorBitmap(Brush brush, int w, int h)
        {
            var bitmap = new Bitmap(w, h);
            try
            {
                using (var g = Graphics.FromImage(bitmap)) FrameBorderRenderer.Instance.RenderToGraphics(g, new Rectangle(0,0,w,h), FrameBorderRenderer.StyleType.DarkHive );
                //using (Graphics g = Graphics.FromImage(bitmap)) g.FillRectangle(brush, 0, 0, w, h);
            }
            catch
            {
                bitmap.Dispose();
                throw;
            }
            return bitmap;
        }

        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(name));
        }

        void RenderPlayers()
        {
            int currentPlayers = Battle.NonSpectatorCount;
            int maxPlayers = Battle.MaxPlayers;

            int friends = 0;
            int admins = 0;
            int mes = 0; // whether i'm in the battle (can be 0 or 1)

            foreach (UserBattleStatus user in Battle.Users.Values)
            {
                if (user.Name == Program.TasClient.UserName) mes++;
                if (Program.FriendManager.Friends.Contains(user.Name)) friends++;
                else if (user.LobbyUser.IsAdmin) admins++;
            }

            // make sure there aren't more little dudes than non-specs in a battle
            while (admins != 0 && friends + admins + mes > Battle.NonSpectatorCount) admins--;
            while (friends != 0 && friends + mes > Battle.NonSpectatorCount) friends--;

            if (playersBoxImage != null) playersBoxImage.Dispose();

            playersBoxImage = DudeRenderer.GetImage(currentPlayers - friends - admins - mes,
                friends,
                admins,
                0,
                maxPlayers,
                mes > 0,
                (int)playersBoxSize.Width,
                (int)playersBoxSize.Height);
        }

        void UpdateImage()
        {
            MakeMinimap();
            RenderPlayers();
            int scaledWidth = (int)Width;
            int scaledHeight = (int)Height;
            image = MakeSolidColorBitmap(BackBrush, scaledWidth, scaledHeight);
            using (Graphics g = Graphics.FromImage(image))
            {
                if (disposed)
                {
                    image = MakeSolidColorBitmap(BackBrush, scaledWidth, scaledHeight);
                    return;
                }
                if (finishedMinimap != null) g.DrawImageUnscaled(finishedMinimap, (int)3, (int)3);
                else
                {
                    g.DrawImage(ZklResources.download,
                        (int)4,
                        (int)3,
                        (int)61,
                        (int)64);
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.InterpolationMode = InterpolationMode.Default;
                }
                g.SetClip(new Rectangle(0, 0, scaledWidth, scaledHeight));
                String mod_and_engine_name = string.Format("{0}     {1}{2}", Battle.ModName, Battle.EngineName, Battle.EngineVersion);
                int y = (int)3;
                int offset = (int)16;
                int curMapCellSize = (int)MapCellSize.Width;
                g.DrawString(Battle.Title, TitleFont, TextBrush, curMapCellSize, y + offset * 0);
                if (g.MeasureString(mod_and_engine_name, ModFont).Width < scaledWidth - curMapCellSize)
                {
                    g.DrawString(mod_and_engine_name, ModFont, TextBrush, curMapCellSize, y + offset * 1);
                    g.DrawImageUnscaled(playersBoxImage, curMapCellSize, y + offset * 2);
                }
                else
                {
                    int offset_offset = (int)4; //this squishes modName & engine-name and dude-icons together abit
                    int offset_offset2 = (int)6; //this squished modName & engine-name into 2 tight lines
                    g.DrawString(Battle.ModName, ModFont, TextBrush, curMapCellSize, y + offset * 1 - offset_offset);
                    g.DrawString(string.Format("{0}{1}", Battle.EngineName, Battle.EngineVersion),
                        ModFont,
                        TextBrush,
                        curMapCellSize,
                        y + offset * 2 - offset_offset - offset_offset2);
                    g.DrawImageUnscaled(playersBoxImage, curMapCellSize, y + offset * 3 - offset_offset - offset_offset2);
                }
                g.ResetClip();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public string ToolTip { get { return ToolTipHandler.GetBattleToolTipString(Battle.BattleID); } }
    }
}