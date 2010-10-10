using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using ZeroKLobby;

namespace ZeroKLobby.MicroLobby
{
    public partial class HelpControl: UserControl
    {
        public HelpControl()
        {
            InitializeComponent();
            feedbackButton.MouseUp += feedbackButton_MouseUp;
            helpButton.MouseUp += helpButton_MouseUp;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        void feedbackButton_MouseUp(object sender, MouseEventArgs ea)
        {
            var menu = new ContextMenu();
            var joinItem = new MenuItem("Chat with us in the Zero-K development channel");
            joinItem.Click += (s, e) => ActionHandler.JoinAndSwitch("zkdev");
            menu.MenuItems.Add(joinItem);
            var siteItem = new MenuItem("Leave us a message on the Zero-K development site");
            siteItem.Click += siteFeatureRequestItem_Click;
            menu.MenuItems.Add(siteItem);
            menu.Show(feedbackButton, ea.Location);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        void helpButton_MouseUp(object sender, MouseEventArgs ea)
        {
            var menu = new ContextMenu();
            var joinItem = new MenuItem("Ask in the developer channel (#sy)");
            joinItem.Click += (s, e) => ActionHandler.JoinAndSwitch("sy");
            menu.MenuItems.Add(joinItem);
            var helpForumItem = new MenuItem("Ask in the Spring Help Forum");
            helpForumItem.Click += helpForumItem_Click;
            menu.MenuItems.Add(helpForumItem);
            var adminsItem = new MenuItem("Ask an Administrator");
            foreach (var admin in Program.TasClient.ExistingUsers.Values.Where(u => u.IsAdmin && !u.IsBot).OrderBy(u => u.IsAway ? 1 : 0))
            {
                var item = new MenuItem(admin.Name + (admin.IsAway ? " (Idle)" : String.Empty));
                var adminName = admin.Name;
                item.Click += (s, e) => ActionHandler.OpenPrivateMessageChannel(adminName);
                adminsItem.MenuItems.Add(item);
            }
            menu.MenuItems.Add(adminsItem);
            menu.Show(helpButton, ea.Location);
        }

        void helpForumItem_Click(object sender, EventArgs e)
        {
            try
            {
                Process.Start("http://springrts.com/phpbb/viewforum.php?f=11");
            }
            catch {}
        }

        void logButton_Click(object sender, EventArgs e)
        {
            ActionHandler.ShowLog();
        }

        void problemButton_Click(object sender, EventArgs e)
        {
            try
            {
                Process.Start("http://code.google.com/p/zero-k/issues/entry");
            }
            catch {}
        }

        void siteFeatureRequestItem_Click(object sender, EventArgs e)
        {
            try
            {
                Process.Start("http://code.google.com/p/zero-k/issues/entry?template=Feature%20Request");
            }
            catch {}
        }

				private void HelpControl_Load(object sender, EventArgs e)
				{
					webBrowser1.Navigate("http://docs.google.com/View?id=ajkvvmht466g_73gmsdwd8r");
				}
    }
}