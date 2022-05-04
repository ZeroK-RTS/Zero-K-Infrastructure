using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using PlasmaShared;
using ZkData;

namespace ZeroKWeb.ForumParser
{
    public class
        AtSignTag: ScanningTag
    {
        public override string Match { get; } = "@";

        public override LinkedListNode<Tag> Translate(TranslateContext context, LinkedListNode<Tag> self) {
            if (self.Previous?.Value is LiteralTag) // previous is contiguous text
            {
                context.Append("@");
                return self.Next;
            }
            var ender = self.Next.FirstNode(x => !(x.Value is LiteralTag || x.Value is UnderscoreTag));
            var text = self.Next.GetOriginalContentUntilNode(ender); // get next string

            if (string.IsNullOrEmpty(text))
            {
                context.Append("@");
                return self.Next;
            }
            int idx;
            for (idx = 0; idx < text.Length; idx++) if (!Utils.ValidLobbyNameCharacter(text[idx])) break;
            string remainder = null;
            string val = text;
            if (idx != 0 && idx < text.Length)
            {
                remainder = text.Substring(idx, text.Length - idx);
                val = text.Substring(0, idx);
            }

            if (string.IsNullOrEmpty(val))
            {
                context.Append("@");
                return self.Next;
            }
            var db = new ZkDataContext();
            char prefix = Char.ToLowerInvariant(val[0]);
            int id = 0;
            int.TryParse(val.Substring(1), out id);

            var fac = db.Factions.FirstOrDefault(x => x.Shortcut == val);
            if (fac == null && prefix == 'f')
            {
                fac = db.Factions.FirstOrDefault(x => x.FactionID == id);
            }
            if (fac != null)
            {
                context.Append(context.Html.PrintFaction(fac, false));
                context.Append(remainder);
                return ender;
            }

            var acc = Account.AccountByName(db, val);
            if (acc == null && prefix == 'u')
            {
                acc = db.Accounts.FirstOrDefault(x => x.AccountID == id);
            }
            if (acc != null)
            {
                context.Append(context.Html.PrintAccount(acc));
                context.Append(remainder);
                return ender;
            }

            var clan = db.Clans.FirstOrDefault(x => x.Shortcut == val);
            if (clan == null && prefix == 'c')
            {
                clan = db.Clans.FirstOrDefault(x => x.ClanID == id);
            }
            if (clan != null)
            {
                context.Append(context.Html.PrintClan(clan));
                context.Append(remainder);
                return ender;
            }

            // can't tag a battle by its name
            SpringBattle bat = null;
            if (prefix == 'b')
            {
                bat = db.SpringBattles.FirstOrDefault(x => x.SpringBattleID == id);
            }
            if (bat != null)
            {
                context.Append(context.Html.PrintBattle(bat));
                context.Append(remainder);
                return ender;
            }

            context.Append("@");
            return self.Next;
        }

        public override Tag Create() => new AtSignTag();
    }
}
