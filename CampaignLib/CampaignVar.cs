using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace CampaignLib
{
    public class CampaignVar
    {
        public string Name { get; set; }
        public string ID { get; set; }
        public string Description { get; set; }
        public Object Value { get; set; }

        public CampaignVar(string id)
        {
            ID = id;
            Value = null;
        }
    }
}
