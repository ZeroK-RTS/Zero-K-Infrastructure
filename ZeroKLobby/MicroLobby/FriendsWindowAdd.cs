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
    public partial class FriendsWindowAdd : Form
    {
        public FriendsWindowAdd()
        {
            InitializeComponent();
        }

        public String getLabelFriendText()
        {
            return labelAddFriend.Text;
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            Program.FriendManager.AddFriend(textBoxAddFriend.Text);            
            Close();
        }
    }
}
