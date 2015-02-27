﻿using System;
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
        const int minimapSize = 58;
        bool dirty;

        bool disposed;
        Bitmap finishedMinimap;
        Bitmap image;
        bool isInGame;


        Bitmap playersBoxImage;
        static Size playersBoxSize = new Size(214, 32);
        Image resizedMinimap;
        public static Brush BackBrush = new SolidBrush(Program.Conf.BgColor);

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
                resizedMinimap = new Bitmap(value, DpiMeasurement.ScaleValueX(minimapSize), DpiMeasurement.ScaleValueY(minimapSize));
                dirty = true;
                OnPropertyChanged("BitmapSource"); // notify wpf about icon change
            }
        }
        public static Font ModFont = new Font("Segoe UI", 8.25F, FontStyle.Regular);
        public int PlayerCount { get { return Battle.NonSpectatorCount; } }


        public static Brush TextBrush = new SolidBrush(Program.Conf.TextColor);
        public static Font TitleFont = new Font("Segoe UI", 8.25F, FontStyle.Bold);

        public static Font QueueFont = new Font("Segoe UI", 13.25F, FontStyle.Bold);
        public static Brush QueueBrush = new SolidBrush(Program.Conf.TextColor);
        public static Brush QueueBrushOutline = new SolidBrush(Program.Conf.BgColor);

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
            finishedMinimap = new Bitmap(DpiMeasurement.ScaleValueX(ZklResources.border.Width), DpiMeasurement.ScaleValueY(ZklResources.border.Height));

            using (Graphics g = Graphics.FromImage(finishedMinimap))
            {
                g.DrawImage(resizedMinimap, DpiMeasurement.ScaleValueX(6), DpiMeasurement.ScaleValueY(5));
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                int x = DpiMeasurement.ScaleValueX(10);
                int y = DpiMeasurement.ScaleValueY(minimapSize - 20);
                Action<Image> drawIcon = image =>
                {
                    g.DrawImage(image, x, y, DpiMeasurement.ScaleValueX(20), DpiMeasurement.ScaleValueY(20));
                    x += DpiMeasurement.ScaleValueX(30);
                };

                if (IsInGame)
                {
                    g.DrawImage(ZklResources.boom,
                        DpiMeasurement.ScaleValueX(10),
                        DpiMeasurement.ScaleValueY(10),
                        DpiMeasurement.ScaleValueX(50),
                        DpiMeasurement.ScaleValueY(50));
                }
                if (Battle.IsOfficial() && Battle.IsSpringieManaged && !Battle.IsQueue)
                {
                    g.DrawImage(ZklResources.star,
                        DpiMeasurement.ScaleValueX(48),
                        DpiMeasurement.ScaleValueY(8),
                        DpiMeasurement.ScaleValueX(15),
                        DpiMeasurement.ScaleValueY(15));
                }
                if (Battle.IsOfficial() && Battle.IsQueue)
                {
                    g.DrawStringWithOutline(Battle.QueueName.Replace(' ', '\n'),
                        QueueFont,
                        QueueBrush,
                        QueueBrushOutline,
                        new Rectangle(4, 4, DpiMeasurement.ScaleValueX(62), DpiMeasurement.ScaleValueY(62)),
                        new StringFormat() { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center},
                        3);
                }

                if (Battle.IsPassworded) drawIcon(ZklResources._lock);

                g.DrawImage(ZklResources.border, 0, 0, DpiMeasurement.ScaleValueX(70), DpiMeasurement.ScaleValueY(70));
            }
        }

        Bitmap MakeSolidColorBitmap(Brush brush, int w, int h)
        {
            var bitmap = new Bitmap(w, h);
            try
            {
                using (Graphics g = Graphics.FromImage(bitmap)) g.FillRectangle(brush, 0, 0, w, h);
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
                DpiMeasurement.ScaleValueX(playersBoxSize.Width),
                DpiMeasurement.ScaleValueY(playersBoxSize.Height));
        }

        void UpdateImage()
        {
            DpiMeasurement.DpiXYMeasurement();
            MakeMinimap();
            RenderPlayers();
            int scaledWidth = DpiMeasurement.ScaleValueX(Width);
            int scaledHeight = DpiMeasurement.ScaleValueY(Height);
            image = MakeSolidColorBitmap(BackBrush, scaledWidth, scaledHeight);
            using (Graphics g = Graphics.FromImage(image))
            {
                if (disposed)
                {
                    image = MakeSolidColorBitmap(BackBrush, scaledWidth, scaledHeight);
                    return;
                }
                if (finishedMinimap != null) g.DrawImageUnscaled(finishedMinimap, DpiMeasurement.ScaleValueX(3), DpiMeasurement.ScaleValueY(3));
                else
                {
                    g.DrawImage(ZklResources.download,
                        DpiMeasurement.ScaleValueX(4),
                        DpiMeasurement.ScaleValueY(3),
                        DpiMeasurement.ScaleValueX(61),
                        DpiMeasurement.ScaleValueY(64));
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.InterpolationMode = InterpolationMode.Default;
                }
                g.SetClip(new Rectangle(0, 0, scaledWidth, scaledHeight));
                String mod_and_engine_name = string.Format("{0}     {1}{2}", Battle.ModName, Battle.EngineName, Battle.EngineVersion);
                int y = DpiMeasurement.ScaleValueY(3);
                int offset = DpiMeasurement.ScaleValueY(16);
                int curMapCellSize = DpiMeasurement.ScaleValueX(MapCellSize.Width);
                g.DrawString(Battle.Title, TitleFont, TextBrush, curMapCellSize, y + offset * 0);
                if (g.MeasureString(mod_and_engine_name, ModFont).Width < scaledWidth - curMapCellSize)
                {
                    g.DrawString(mod_and_engine_name, ModFont, TextBrush, curMapCellSize, y + offset * 1);
                    g.DrawImageUnscaled(playersBoxImage, curMapCellSize, y + offset * 2);
                }
                else
                {
                    int offset_offset = DpiMeasurement.ScaleValueY(4); //this squishes modName & engine-name and dude-icons together abit
                    int offset_offset2 = DpiMeasurement.ScaleValueY(6); //this squished modName & engine-name into 2 tight lines
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