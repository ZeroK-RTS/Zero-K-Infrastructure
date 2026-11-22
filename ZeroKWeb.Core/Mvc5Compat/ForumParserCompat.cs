using System;

namespace System.Web.Mvc.Html
{
    // Named by a using in ForumParser/Tags/WikiLinkTag.cs and never otherwise mentioned.
    internal static class DeadUsingMarker { }
}

namespace Microsoft.Ajax.Utilities
{
    // Same: a dead using in WikiLinkTag.cs, left behind by the IDE.
    internal static class DeadUsingMarker { }
}

namespace Antlr.Runtime.Misc
{
    /// <summary>
    /// ForumParser/ParserExtensions.cs takes <c>Antlr.Runtime.Misc.Func&lt;T, bool&gt;</c> as
    /// a parameter type in four places. That is Antlr's own delegate, not System.Func, and
    /// nothing else in the file suggests it was meant - the IDE resolved a bare <c>Func</c>
    /// against whatever was in scope and the code has carried it since.
    ///
    /// Declared here with Antlr's exact shape so the linked parser compiles and its lambdas
    /// bind as they always have. The real fix is to write System.Func in that file, which
    /// needs someone who can build the Framework project to confirm nothing else depends on
    /// the odd type - the same reason WebGrease is supplied rather than deleted.
    /// </summary>
    public delegate TResult Func<in T, out TResult>(T arg);
}

namespace ZkData.Migrations
{
    // Named by a using in ForumParser/Tags/Abstract/Tag.cs and never otherwise mentioned.
    // The real ZkData.Migrations holds the EF6 migrations, which ZkData.Core does not link.
    internal static class DeadUsingMarker { }
}

namespace JetBrains.Annotations
{
    /// <summary>
    /// TranslateContext.cs marks AppendFormat with [StringFormatMethod("formatString")],
    /// from the JetBrains.Annotations package the web project references.
    ///
    /// It is not simply missing here: one of the port's own package dependencies ships an
    /// INTERNAL copy of the same namespace, so the name resolves and then fails as
    /// inaccessible. Declaring a public one in this assembly settles it.
    ///
    /// The attribute carries no behaviour - it tells an IDE that a parameter is a format
    /// string - so an empty implementation is the whole implementation.
    /// </summary>
    [AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Method |
                    AttributeTargets.Property | AttributeTargets.Delegate)]
    public sealed class StringFormatMethodAttribute : Attribute
    {
        public StringFormatMethodAttribute(string formatParameterName)
            => FormatParameterName = formatParameterName;

        public string FormatParameterName { get; }
    }
}
