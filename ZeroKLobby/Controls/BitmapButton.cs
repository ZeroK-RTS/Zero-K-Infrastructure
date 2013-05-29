using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ZeroKLobby
{
    public class BitmapButton: Button
    {
        public BitmapButton() {
            //BackColor = Color.DarkSlateGray;
            BackColor = Color.Transparent;
            BackgroundImage = Buttons.panel;
            FlatStyle = FlatStyle.Flat;
            BackgroundImageLayout = ImageLayout.Stretch;
            ForeColor = Color.White;
            FlatAppearance.BorderSize = 0;
            FlatAppearance.MouseDownBackColor = Color.DarkSlateGray;
            FlatAppearance.MouseOverBackColor = Color.AliceBlue;
        }
    }
}