using System.Windows.Forms;

namespace PlanetWars.UI
{
    public class CustomPropertyGrid : PropertyGrid
    {
        public CustomPropertyGrid()
        {
            ToolStripRenderer = new ToolStripProfessionalRenderer();
            base.HelpVisible = false;
            base.ToolbarVisible = false;
            base.Dock = DockStyle.Fill;
            PropertySort = PropertySort.Alphabetical;
            // HelpBackColor
            // HelpForeColor
        }

        public CustomPropertyGrid(object o) : this()
        {
            SelectedObject = o;
        }
    }
}