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
