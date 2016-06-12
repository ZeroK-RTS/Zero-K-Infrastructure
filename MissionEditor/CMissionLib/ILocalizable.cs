using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMissionLib
{
    // note: add any implementations of this to MissionEditor2.MainWindow.xaml.cs.ExportLocalizationFile() as well
    public interface ILocalizable
    {
        string StringID { get; set; }
    }
}
