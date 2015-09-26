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
        
        public override LinkedListNode<Tag> Translate(TranslateContext context, LinkedListNode<Tag> self) {

            if (self.Previous == null || self.Previous.Value is SpaceTag || self.Previous.Value is NewLineTag) // previous is space or newline
            {
                var nextLit = self.Next?.Value as LiteralTag; // next is string
                if (nextLit != null)
                {
                    var val = self.Next.GetOriginalContentWhileCondition(x=>x.Value is LiteralTag || x.Value is StarTag || x.Value is UnderscoreTag); // get next string
                    var db = new ZkDataContext();

                    var acc = Account.AccountByName(db, val);
                    if (acc != null)
                    {
                        context.Append(context.Html.PrintAccount(acc));
                        return self.Next.Next;
                    }
                    var clan = db.Clans.FirstOrDefault(x => x.Shortcut == val);
                    if (clan != null)
                    {
                        context.Append(context.Html.PrintClan(clan));
                        return self.Next.Next;
                    }
                    var fac = db.Factions.FirstOrDefault(x => x.Shortcut == val);
                    if (fac != null)
                    {
                        context.Append(context.Html.PrintFaction(fac, false));
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
                                context.Append(context.Html.PrintBattle(bat));
                                return self.Next.Next;
                            }
                        }
                    }
                }
            }
            context.Append("@");
            return self.Next;
        }

        public override Tag Create() => new AtSignTag();
    }
}