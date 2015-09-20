using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace PlasmaShared.ForumParser
{
    public abstract class OpeningTag<T>: ScanningTag where T:Tag
    {
    }
}