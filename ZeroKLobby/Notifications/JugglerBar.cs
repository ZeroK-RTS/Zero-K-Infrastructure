using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using LobbyClient;

namespace ZeroKLobby.Notifications
{
    public partial class JugglerBar : UserControl, INotifyBar
    {
        TasClient client;
        public NotifyBarContainer BarContainer { get; private set; }

        public JugglerBar(TasClient client)
        {
            InitializeComponent();
            this.client = client;
        }

        public void AddedToContainer(NotifyBarContainer container)
        {
            BarContainer = container;
            container.btnDetail.Text = "MatchMaking";
            container.btnDetail.Enabled = false;
        }

        public void CloseClicked(NotifyBarContainer container)
        {
            Program.NotifySection.RemoveBar(this);
        }

        public void DetailClicked(NotifyBarContainer container)
        {
            
        }

        public Control GetControl()
        {
            return this;
        }
    }
}
