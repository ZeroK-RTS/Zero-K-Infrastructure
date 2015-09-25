using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ZeroKWeb.ForumParser
{
    public static class ParserExtensions
    {
        public static LinkedListNode<Tag> NextNodeOfType<T>(this LinkedListNode<Tag> node)
        {
            while (node != null && !(node.Value is T)) node = node.Next;
            return node;
        }
    }
}