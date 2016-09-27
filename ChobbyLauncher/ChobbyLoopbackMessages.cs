using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChobbyLauncher
{
    public class ChobbyMessageAttribute : Attribute {}
    
    [ChobbyMessage]
    public class OpenUrl
    {
        public string Url { get; set; }
    }

    [ChobbyMessage]
    public class OpenFolder
    {
        public string Folder { get; set; }
    }

}
