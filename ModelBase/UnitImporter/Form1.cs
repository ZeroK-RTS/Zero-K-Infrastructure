using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using ModelBase;

namespace UnitImporter
{
    public partial class Form1 : Form
    {
        const string ImportGameName = "ca";
        const string SpringPath = @"C:\Programy\hry\Spring";
        const string hta = @"c:\programy\Hry\spring\mods\ca-stable-7761.sdz";
         readonly string[] morphedUnitNames = new[] {"amgeo", "cmgeo", "cormortgold"};
        
        
        
        public Form1()
        {
            InitializeComponent();
            var mod = Mod.FromPath(SpringPath, hta);

            using (var db = new DatabaseDataContext()) {

                var game = db.Games.Where(x => x.Shortcut == ImportGameName).Single();

                foreach (var ud in mod.Units) if (IsValid(ud))
                {
                    if (ud.Description == null || ud.Name == null || ud.FullName == null || ud.Description == null) throw new Exception("field is null");
                    var u = game.Units.Where(x => x.Code == ud.Name).SingleOrDefault();
                    if (u == null) u = new Unit() {GameID = game.GameID};
                    u.Code = ud.Name;
                    u.Name = ud.FullName;
                    u.Description = ud.Description;
                    if (ud.Parent != null) u.ParentCode = ud.Parent.Name;
                    if (u.UnitID == 0) db.Units.InsertOnSubmit(u); 
                    db.SubmitChanges();
                }

                foreach (var u in game.Units) {
                    if (!mod.Units.Any(x=>x.Name == u.Code && IsValid(x))) {
                        db.Units.DeleteOnSubmit(u);
                    }
                }

                db.SubmitChanges();


            }

            MessageBox.Show("success");
        }

        bool IsValid(UnitDef unit)
        {
            if (morphedUnitNames.Contains(unit.Name)) return true;
            if (unit.Parent != null) return true;
            return false;
        }


        void button1_Click(object sender, EventArgs e)
        {

        }
    }
}