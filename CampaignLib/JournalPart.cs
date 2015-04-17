using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace CampaignLib
{
    public class JournalPart
    {
        public string Name { get; set; }
        public string ID { get; set; }
        public string Text { get; set; }
        public string Image { get; set; }
        
        public Dictionary<String, Object> VariablesRequired { get; set; }

        public JournalPart(string id)
        {
            ID = id;
            VariablesRequired = new Dictionary<String, object>();
        }

        public bool Accept()
        {
            foreach (var testVar in VariablesRequired)
            {
                // TODO: get campaign vars
            }
            return true;
        }
    }
}
