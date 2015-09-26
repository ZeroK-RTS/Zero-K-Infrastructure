using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Antlr.Runtime.Misc;

namespace ZeroKWeb.ForumParser
{
    public static class ParserExtensions
    {
        public static LinkedListNode<Tag> NextNodeOfType<T>(this LinkedListNode<Tag> node) {
            while (node != null && !(node.Value is T)) node = node.Next;
            return node;
        }


        public static string GetOriginalContentWhileCondition(this LinkedListNode<Tag> node, Func<LinkedListNode<Tag>, bool> whileCondition) {
            var sb = new StringBuilder();
            while (node != null && whileCondition(node))
            {
                sb.Append(node.Value.GetOriginalContent());
                node = node.Next;
            }
            return sb.ToString();
        }

        public static string GetOriginalContentUntilTag<T>(this LinkedListNode<Tag> node) {
            return node.GetOriginalContentWhileCondition(x => !(x.Value is T));
        }

        public static string GetOriginalContentUntilNode(this LinkedListNode<Tag> startNode, LinkedListNode<Tag> endNode) {
            return startNode.GetOriginalContentWhileCondition(x => x != endNode);
        }

        public static string GetOriginalContentUntilWhiteSpaceOrEndline(this LinkedListNode<Tag> node) {
            return node.GetOriginalContentWhileCondition(x => !(x.Value is SpaceTag) && !(x.Value is NewLineTag));
        }

        public static void TranslateWhile(this LinkedListNode<Tag> startNode, TranslateContext context, Func<LinkedListNode<Tag>, bool> condition) {
            var n = startNode;
            while (n != null && condition(n)) n = n.Value.Translate(context, n);
        }

        public static LinkedListNode<Tag> FirstNode(this LinkedListNode<Tag> startNode, Func<LinkedListNode<Tag>, bool> condition) {
            while (startNode != null)
            {
                if (condition(startNode)) return startNode;
                startNode = startNode.Next;
            }
            return null;
        }

        public static LinkedListNode<Tag> FirstNodeReverse(this LinkedListNode<Tag> startNode, Func<LinkedListNode<Tag>, bool> condition)
        {
            while (startNode != null)
            {
                if (condition(startNode)) return startNode;
                startNode = startNode.Previous;
            }
            return null;
        }


        public static void TranslateUntilNode(this LinkedListNode<Tag> startNode, TranslateContext context, LinkedListNode<Tag> endNode) {
            startNode.TranslateWhile(context, x => x != endNode);
        }

        public static bool IsValidLink(this string content) {
            return Regex.IsMatch(content, "(mailto|spring|http|https|ftp|ftps|zk)\\://[^\\\"']+$", RegexOptions.IgnoreCase);
        }
    }
}