using System;

namespace PlanetWarsShared
{
    [Serializable]
    public class PurchaseData
    {
        public PurchaseData() {}

        public PurchaseData(int upgradeDefID, string unitChoice)
        {
            Date = DateTime.Now;
            UpgradeDefID = upgradeDefID;
            UnitChoice = unitChoice;
        }

        public DateTime Date { get; set; }
        public int UpgradeDefID { get; set; }
        public string UnitChoice { get; set; }
    }
}