using System;

namespace ZkData
{
    public class KudosPurchase
    {
        public int KudosPurchaseID { get; set; }
        public int AccountID { get; set; }
        public int KudosValue { get; set; }
        public DateTime Time { get; set; }
        public int? UnlockID { get; set; }

        public virtual Account Account { get; set; }
        public virtual Unlock Unlock { get; set; }
    }
}
