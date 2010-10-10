using System.Windows.Forms;
using PlanetWars.Utility;

namespace PlanetWars.UI
{
    class MapInfoView : ListView
    {
        const int MapSizeDiv = 512; // spring distance units in 1 tasclient unit

        public MapInfoView()
        {
            View = View.Details;
            base.Dock = DockStyle.Fill;
            Height = 200;
            Columns.AddRange(new[] {new ColumnHeader {Text = "Property"}, new ColumnHeader {Text = "Value"},});
        }

        public MapInfoView(Map map) : this()
        {
            UpdateListView(map);
        }

        public void UpdateListView(Map map)
        {
            BeginUpdate();
            try {
                Items.Clear();
                AddMapInfoItem(map.Name, "Name");
                if (!map.Author.IsNullOrEmpty()) {
                    AddMapInfoItem(map.Author, "Author");
                }
                if (!map.Description.IsNullOrEmpty()) {
                    AddMapInfoItem(map.Description, "Description");
                }
                AddMapInfoItem(map.Size.Width/MapSizeDiv, "Width");
                AddMapInfoItem(map.Size.Height/MapSizeDiv, "Height");
                AddMapInfoItem(map.Gravity, "Gravity");
                AddMapInfoItem(map.ExtractorRadius, "Metal Extractor Radius");
                AddMapInfoItem(map.MaxMetal, "Maximum Metal");
                AddMapInfoItem(map.MaxWind, "Maximum Wind");
                AddMapInfoItem(map.MinWind, "Minimum Wind");
                AddMapInfoItem(map.TidalStrength, "Tidal Strength");
                Columns[0].AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
                Columns[1].AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
            } finally {
                EndUpdate();
            }
        }

        void AddMapInfoItem(object value, string property)
        {
            ListViewItem listItem = new ListViewItem(property);
            listItem.SubItems.Add(value.ToString());
            Items.Add(listItem);
        }
    }
}