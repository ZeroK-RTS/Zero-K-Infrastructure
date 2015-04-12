using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace CampaignLib
{
    [DataContract]
    public class CampaignVar
    {
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public string ID { get; set; }
        [DataMember]
        public string Description { get; set; }
        [DataMember]
        public Object Value { get; set; }

        public CampaignVar(string id)
        {
            ID = id;
            Value = null;
        }
    }
}
