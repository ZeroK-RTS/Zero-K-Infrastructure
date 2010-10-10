using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Xml.Linq;

using System.Text;
using PlanetWarsShared;

public partial class awardhall : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        DisplayAwardHOF();
    }

    private void DisplayAwardHOF()
    {
        List<awardRecord> awardRecords = new List<awardRecord>();
        foreach (Player p in Globals.Galaxy.Players)
        {
            foreach (Award a in p.Awards)
            {
                double curAwardVal = extractRecordVal(a.Text);
                int index = awardRecords.FindIndex(delegate(awardRecord ar) { return ar.recordAward.Type.Equals(a.Type); });
                if (index == -1)
                {
                    awardRecords.Add(new awardRecord(a, p, curAwardVal));
                }
                else
                {
                    awardRecords[index].curCollectorVal++;
                    if (curAwardVal > awardRecords[index].recordVal)
                    {
                        awardRecords[index].recordVal = curAwardVal;
                        awardRecords[index].recordAward = a;
                        awardRecords[index].recordPlayer = p;
                    }
                }
            }
            foreach (awardRecord ar in awardRecords)
            {
                if (ar.curCollectorVal > ar.collectorVal)
                {
                    ar.collectorVal = ar.curCollectorVal;
                    ar.collectorPlayers.Clear();
                    ar.collectorPlayers.Add(p);
                }
                else if (ar.curCollectorVal >= ar.collectorVal)
                {
                    ar.collectorPlayers.Add(p);
                }

                ar.curCollectorVal = 0;
            }
        }

        var sb = new StringBuilder();
        sb.AppendFormat("<table cellspacing='10'>\n");
        foreach (awardRecord ar in awardRecords)
        {
            sb.AppendFormat("{0}\n", ar);
        }
        sb.AppendFormat("</table>\n");
        awardhof.Text = sb.ToString();
        
    }
    private double extractRecordVal(string record)
    {
        string recordValStr = "";
        
        foreach (var curChar in record)
                if (Char.IsDigit(curChar) || curChar == '.') recordValStr += curChar;

        try
        {
            return double.Parse(recordValStr.Trim('.'));
        }
        catch(FormatException e)
        {
            return 0;
        }
    }

    private class awardRecord
    {
        public Award recordAward;
        
        public double recordVal;
        public Player recordPlayer;
        public int collectorVal;
        public List<Player> collectorPlayers = new List<Player>();

        public int curCollectorVal;
        public int curCollectorPlayerName;

        public awardRecord(Award recordAward, Player player, double recordVal)
        {
            this.recordAward = recordAward;
            this.recordVal = recordVal;
            this.collectorVal = 0;
            this.recordPlayer = player;
            this.collectorPlayers.Add(player);

            this.curCollectorVal = 1;
        }
        
        private string printCollectorPlayers()
        {
            string players = "";
            foreach (Player p in collectorPlayers)
            {
                players += p.ToHtml();
            }
            return players;
        }

        public override string ToString()
        {
            return string.Format("<tr><td><img src='awards/trophy_{0}.png' style='border:2px solid {5}'></td><td>Galactic Record: {1} ({2}) <br />\n Top Collector(s) {3} ({4} collected)</td></tr>", recordAward.Type, recordPlayer.ToHtml(), recordAward.Text, this.printCollectorPlayers(), collectorVal, recordAward.Type != "friend" ? "green" : "red");
        }
    }

}
