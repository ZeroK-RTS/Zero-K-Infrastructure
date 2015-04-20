using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CampaignLib;

namespace ZeroKLobby.MicroLobby.Campaign
{
    public partial class CampaignControl : UserControl, INavigatable
    {
        CampaignManager manager;
        List<Planet> planetsToRender = new List<Planet>();
        Dictionary<string, BitmapButton> buttons = new Dictionary<string, BitmapButton>();

        public CampaignControl()
        {
            InitializeComponent();
            manager = new CampaignManager();
            manager.LoadCampaign("test");
            manager.LoadCampaignSave("test", "save1");

            planetsToRender = manager.GetUnlockedPlanets();
            foreach (Planet planet in planetsToRender)
            {
                BitmapButton button = new BitmapButton();
                button.BackgroundImage = new Bitmap("images/planets/terran1.png");
                button.Parent = this;
                button.Width = 48;
                button.Height = 48;
                button.Location = new Point((int)planet.X, (int)planet.Y);
                //button.
            }
        }

        // from INavigatable
        public string Title { get {return "Campaign galaxy"; } }
        public string PathHead { get { return "campaign"; } }
        public bool Hilite(HiliteLevel level, string path)
        {
            return false;
        }
        public bool TryNavigate(params string[] path)
        {
            return path.Length > 0 && path[0] == PathHead;
        }
    }
}
