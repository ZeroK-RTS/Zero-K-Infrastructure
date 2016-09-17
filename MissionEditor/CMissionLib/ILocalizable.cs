using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMissionLib
{
    /// <summary>
    /// Implemented by classes that have localizable strings
    /// Note: add any implementations of this to <see cref="MissionEditor2.MainWindow.xaml.cs.ExportLocalizationFile()"/> as well
    /// </summary>
    public interface ILocalizable
    {
        string StringID { get; set; }
    }
}
