﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace ZeroKWeb.ForumParser
{
    public static class ParserExtensions
    {
        public static LinkedListNode<Tag> NextNodeOfType<T>(this LinkedListNode<Tag> node)
        {
            while (node != null && !(node.Value is T)) node = node.Next;
            return node;
        }


        public static string GetOriginalContentWhileCondition(this LinkedListNode<Tag> node, Antlr.Runtime.Misc.Func<LinkedListNode<Tag>, bool> whileCondition)
        {
            var sb = new StringBuilder();
            while (node != null && whileCondition(node))
            {
                sb.Append(node.Value.Text);
                node = node.Next;
            }
            return sb.ToString();
        }

        public static string GetOriginalContentUntilTag<T>(this LinkedListNode<Tag> node)
        {
            return node.GetOriginalContentWhileCondition(x => !(x.Value is T));
        }

        public static string GetOriginalContentUntilNode(this LinkedListNode<Tag> startNode, LinkedListNode<Tag> endNode)
        {
            return startNode.GetOriginalContentWhileCondition(x => x != endNode);
        }

        public static string GetOriginalContentUntilWhiteSpaceOrEndline(this LinkedListNode<Tag> node)
        {
            return node.GetOriginalContentWhileCondition(x => !(x.Value is SpaceTag) && !(x.Value is NewLineTag));
        }

        public static LinkedListNode<Tag> TranslateWhile(this LinkedListNode<Tag> startNode, TranslateContext context, Antlr.Runtime.Misc.Func<LinkedListNode<Tag>, bool> condition)
        {
            var n = startNode;
            while (n != null && condition(n)) n = n.Value.Translate(context, n);
            return n;
        }

        public static LinkedListNode<Tag> FirstNode(this LinkedListNode<Tag> startNode, Antlr.Runtime.Misc.Func<LinkedListNode<Tag>, bool> condition)
        {
            while (startNode != null)
            {
                if (condition(startNode)) return startNode;
                startNode = startNode.Next;
            }
            return null;
        }

        public static LinkedListNode<Tag> FirstNodeReverse(this LinkedListNode<Tag> startNode, Antlr.Runtime.Misc.Func<LinkedListNode<Tag>, bool> condition)
        {
            while (startNode != null)
            {
                if (condition(startNode)) return startNode;
                startNode = startNode.Previous;
            }
            return null;
        }


        public static IEnumerable<LinkedListNode<T>> AsEnumerable<T>(this LinkedListNode<T> startNode)
        {
            return new LinkedListNodeEnumerator<T>(startNode);
        }

        public static IEnumerable<LinkedListNode<T>> AsReverseEnumerable<T>(this LinkedListNode<T> startNode)
        {
            return new LinkedListNodeBackEnumerator<T>(startNode);
        }


        public static LinkedListNode<Tag> TranslateUntilNode(this LinkedListNode<Tag> startNode, TranslateContext context, LinkedListNode<Tag> endNode)
        {
            return startNode.TranslateWhile(context, x => x != endNode);
        }

        static Regex linkMatcher = new Regex("^(mailto|spring|http|https|ftp|ftps|zk)\\://[^\\\"']+$", RegexOptions.Compiled);

        public static bool IsValidLink(this string content)
        {
            if (string.IsNullOrEmpty(content)) return false;
            return linkMatcher.IsMatch(content);
        }

        public static bool IsValidLinkOrRelativeUrl(this string content, bool imageOnly)
        {
            if (string.IsNullOrEmpty(content)) return false;
            if (!linkMatcher.IsMatch(content)) return false;

            Uri parsed;
            if (Uri.TryCreate(content, UriKind.RelativeOrAbsolute, out parsed) && parsed != null)
            {
                if (imageOnly)
                {
                    // for proper check do HEAD request and check response, but this needs some cache 
                    if (!(parsed.AbsolutePath.EndsWith(".jpg") || parsed.AbsolutePath?.EndsWith(".gif") == true || parsed.AbsolutePath.EndsWith(".png") ||
                          parsed.AbsolutePath.EndsWith(".jpeg"))) return false;
                }

                return true;
            }
            return false;
        }

        public static char ToLower(this char high)
        {
            if (high >= 'A' && high <= 'Z') return (char)(high - 'A' + 'a');
            return high;
        }
    }
}