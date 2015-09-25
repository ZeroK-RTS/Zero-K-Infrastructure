using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using ZkData;

namespace ZeroKWeb.ForumParser
{
    public class AtSignTag: ScanningTag
    {
        public override string Match { get; } = "@";

        public override LinkedListNode<Tag> Translate(StringBuilder sb, LinkedListNode<Tag> self, HtmlHelper html) {

            if (self.Previous == null || self.Previous.Value is SpaceTag || self.Previous.Value is NewLineTag) // previous is space or newline
            {
                var nextLit = self.Next?.Value as LiteralTag; // next is string
                if (nextLit != null)
                {
                    var val = nextLit.Content.ToString(); // get next string
                    var db = new ZkDataContext();

                    var acc = Account.AccountByName(db, val);
                    if (acc != null)
                    {
                        sb.Append(html.PrintAccount(acc));
                        return self.Next.Next;
                    }
                    var clan = db.Clans.FirstOrDefault(x => x.Shortcut == val);
                    if (clan != null)
                    {
                        sb.Append(html.PrintClan(clan));
                        return self.Next.Next;
                    }
                    var fac = db.Factions.FirstOrDefault(x => x.Shortcut == val);
                    if (fac != null)
                    {
                        sb.Append(html.PrintFaction(fac, false));
                        return self.Next.Next;
                    }
                    if (val.StartsWith("b", StringComparison.InvariantCultureIgnoreCase))
                    {
                        var bid = 0;
                        if (int.TryParse(val.Substring(1), out bid))
                        {
                            var bat = db.SpringBattles.FirstOrDefault(x => x.SpringBattleID == bid);
                            if (bat != null)
                            {
                                sb.Append(html.PrintBattle(bat));
                                return self.Next.Next;
                            }
                        }
                    }
                }
            }
            sb.Append("@");
            return self.Next;
        }

        public override Tag Create() => new AtSignTag();
    }
}