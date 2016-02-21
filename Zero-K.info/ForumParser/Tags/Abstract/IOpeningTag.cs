using System;

namespace ZeroKWeb.ForumParser
{
    public interface IOpeningTag
    {
        Type ClosingTagType { get; }
    }
}