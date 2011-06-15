using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ZeroKLobby.MicroLobby
{
    public partial class FriendsWindow : Form
    {
        bool isSelected = false;
        private static bool creatable = true;

        public FriendsWindow()
        {
            if (creatable)
            {
                InitializeComponent();
                refreshItems();
                creatable = false;
            }
        }
        

        public static bool Creatable
        {
            get { return creatable; }
            set { creatable = value; }
        }

        public void clearItemlist()
        {
            this.listBox1.Items.Clear();
        }

        public void refreshItems()
        {
            labelFriends.Text = "Total friends: " + Program.Conf.Friends.Count;

            foreach (String s in Program.FriendManager.Friends)
                {
                    if (Program.TasClient.ExistingUsers.ContainsKey(s))
                    {
                        listBox1.Items.Add(s + " (online)");
                    }
                    else
                    {
                        listBox1.Items.Add(s);
                    }
                }
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            isSelected = true;
        }

        private void listBox1_DoubleClick(object sender, EventArgs e)
        {
            if (isSelected)
            {
                String itemChosen = listBox1.SelectedItem.ToString();
                if(itemChosen.Contains(" (online)"))
                {
                    NavigationControl.Instance.Path = "chat/user/" + itemChosen.Substring(0, itemChosen.ToString().IndexOf(" (online)"));
                }
                else {
                    NavigationControl.Instance.Path = "chat/user/" + itemChosen;
                }
            }
        }

        private void FriendsWindow_FormClosed(object sender, FormClosedEventArgs e)
        {
            Creatable = true;
        }

        private void buttonAddFriend_Click(object sender, EventArgs e)
        {
            FriendsWindowAdd frdWdAdd = new FriendsWindowAdd();
            frdWdAdd.ShowDialog();
        }

        private void listBox1_MouseDown(object sender, MouseEventArgs e)
        {
            try
            {
                if (listBox1.SelectedItem != null && e.Button == MouseButtons.Right)
                {
                    String itemChosen = listBox1.SelectedItem.ToString().Substring(0, listBox1.SelectedItem.ToString().IndexOf(" (online"));

                    var user = Program.TasClient.ExistingUsers.Values.SingleOrDefault(x => x.Name.ToString().ToUpper() == itemChosen.ToUpper());
                    var cm = ContextMenus.GetPlayerContextMenu(user, false);
                    cm.Show(this, new Point(e.X + 10, e.Y + 20));
                }
            }
            catch { }
        }
    }
}
