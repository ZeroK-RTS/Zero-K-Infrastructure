using System.Collections.Generic;
using System.Text;
using Antlr.Runtime.Misc;

namespace ZeroKWeb.ForumParser
{
    public static class ParserExtensions
    {
        public static LinkedListNode<Tag> NextNodeOfType<T>(this LinkedListNode<Tag> node) {
            while (node != null && !(node.Value is T)) node = node.Next;
            return node;
        }


        public static string GetOriginalContentWhileCondition(this LinkedListNode<Tag> node, Func<LinkedListNode<Tag>, bool> whileCondition)
        {
            var sb = new StringBuilder();
            while (node != null && whileCondition(node))
            {
                sb.Append(node.Value.GetOriginalContent());
                node = node.Next;
            }
            return sb.ToString();
        }

        public static string GetOriginalContentUntilTag<T>(this LinkedListNode<Tag> node) {
            return node.GetOriginalContentWhileCondition(x=>!(x.Value is T));
        }

        public static string GetOriginalContentUntilNode(this LinkedListNode<Tag> startNode, LinkedListNode<Tag> endNode) {
            return startNode.GetOriginalContentWhileCondition(x=>x != endNode);
        }

        public static string GetOriginalContentUntilWhiteSpaceOrEndline(this LinkedListNode<Tag> node)
        {
            return node.GetOriginalContentWhileCondition(x => !(x.Value is SpaceTag) && !(x.Value is NewLineTag));
        }

    }
}