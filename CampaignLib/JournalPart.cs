using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace CampaignLib
{
    [DataContract]
    public class JournalPart
    {
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public string ID { get; set; }
        [DataMember]
        public string Text { get; set; }
        [DataMember]
        public string Image { get; set; }
        [DataMember]
        public Dictionary<CampaignVar, Object> VariablesRequired { get; set; }

        public JournalPart(string id)
        {
            ID = id;
            VariablesRequired = new Dictionary<CampaignVar, object>();
        }

        public bool Accept()
        {
            foreach (var testVar in VariablesRequired)
            {
                // left is the var actually stored in campaign records; right is what it ought to be
                if (testVar.Key.Value != testVar.Value) return false;
            }
            return true;
        }
    }
}
