using System;

namespace ZeroKWeb.ForumParser
{
    public abstract class ScanningTag: Tag
    {
        public abstract string Match { get; }

        protected int pos = 0;

        public override bool? ScanLetter(char letter)
        {
            if (Char.ToLower(Match[pos++]) != Char.ToLower(letter)) return false;
            if (pos == Match.Length) return true;
            return null;
        }

    }
}