using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace PlasmaShared.ForumParser
{
    public abstract class OpeningTag: ScanningTag
    {
        public abstract Type ClosingTagType { get; }
    }

    public abstract class OpeningTag<T>: OpeningTag
    {
        public override Type ClosingTagType => typeof(T);
    }
}