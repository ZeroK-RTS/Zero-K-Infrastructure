using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace ZkData
{
    public partial class FactionTreaty
    {
        
        public FactionTreaty()
        {
            TreatyEffects = new HashSet<TreatyEffect>();
        }

        public int FactionTreatyID { get; set; }
        public int ProposingFactionID { get; set; }
        public int ProposingAccountID { get; set; }
        public int AcceptingFactionID { get; set; }
        public int? AcceptedAccountID { get; set; }
        public int? TurnsRemaining { get; set; }
        public TreatyState TreatyState { get; set; }
        public int? TurnsTotal { get; set; }
        [StringLength(1000)]
        public string TreatyNote { get; set; }

        public virtual Account AccountByProposingAccountID { get; set; }
        public virtual Account AccountByAcceptedAccountID { get; set; }
        public virtual Faction FactionByProposingFactionID { get; set; }
        public virtual Faction FactionByAcceptingFactionID { get; set; }
        public virtual ICollection<TreatyEffect> TreatyEffects { get; set; }
        

        public bool CanCancel(Account account)
        {
            if (account == null) return false;
            if (!account.HasFactionRight(x => x.RightDiplomacy)) return false;
            if (TurnsRemaining == null || TreatyState == TreatyState.Proposed)
            {
                if (ProposingFactionID == account.FactionID || AcceptingFactionID == account.FactionID) return true; // can canel
            }
            return false;
        }

        public override string ToString()
        {
            return "TR" + FactionTreatyID;
        }

        public bool CanAccept(Account account)
        {
            if (account == null) return false;
            if (!account.HasFactionRight(x => x.RightDiplomacy)) return false;
            if (TreatyState == TreatyState.Proposed && AcceptingFactionID == account.FactionID) return true;
            return false;
        }

        public bool ProcessTrade(bool oneTimeOnly)
        {
            var fac1 = FactionByProposingFactionID;
            var fac2 = FactionByAcceptingFactionID;

            double metalFac1toFac2 = 0;
            double energyFac1toFac2 = 0;
            double dropshipsFac1toFac2 = 0;
            double bombersFac1toFac2 = 0;
            double warpsFac1toFac2 = 0;


            foreach (var te in TreatyEffects.Where(x => x.TreatyEffectType.IsOneTimeOnly == oneTimeOnly))
            {
                var tr = te.TreatyEffectType;
                if (tr.EffectGiveMetal == true)
                {
                    if (fac1 == te.FactionByGivingFactionID) metalFac1toFac2 += te.Value ?? 0;
                    else metalFac1toFac2 -= te.Value ?? 0;
                }

                if (tr.EffectGiveEnergy == true)
                {
                    if (fac1 == te.FactionByGivingFactionID) energyFac1toFac2 += te.Value ?? 0;
                    else energyFac1toFac2 -= te.Value ?? 0;
                }

                if (tr.EffectGiveDropships == true)
                {
                    if (fac1 == te.FactionByGivingFactionID) dropshipsFac1toFac2 += te.Value ?? 0;
                    else dropshipsFac1toFac2 -= te.Value ?? 0;
                }

                if (tr.EffectGiveBombers == true)
                {
                    if (fac1 == te.FactionByGivingFactionID) bombersFac1toFac2 += te.Value ?? 0;
                    else bombersFac1toFac2 -= te.Value ?? 0;
                }

                if (tr.EffectGiveWarps == true)
                {
                    if (fac1 == te.FactionByGivingFactionID) warpsFac1toFac2 += te.Value ?? 0;
                    else warpsFac1toFac2 -= te.Value ?? 0;
                }

            }

            if (fac1.Metal < metalFac1toFac2 || fac2.Metal < -metalFac1toFac2) return false;
            if (fac1.EnergyProducedLastTurn < energyFac1toFac2 || fac2.EnergyProducedLastTurn < -energyFac1toFac2) return false;
            if (fac1.Dropships < dropshipsFac1toFac2 || fac2.Dropships < -dropshipsFac1toFac2) return false;
            if (fac1.Bombers < bombersFac1toFac2 || fac2.Bombers < -bombersFac1toFac2) return false;
            if (fac1.Warps < warpsFac1toFac2 || fac2.Warps < -warpsFac1toFac2) return false;




            fac1.ProduceMetal(-metalFac1toFac2);
            fac2.ProduceMetal(metalFac1toFac2);
            fac1.EnergyProducedLastTurn -= energyFac1toFac2;
            fac2.EnergyProducedLastTurn += energyFac1toFac2;
            fac1.ProduceDropships(-dropshipsFac1toFac2);
            fac2.ProduceDropships(dropshipsFac1toFac2);
            fac1.ProduceBombers(-bombersFac1toFac2);
            fac2.ProduceBombers(bombersFac1toFac2);
            fac1.ProduceWarps(-warpsFac1toFac2);
            fac2.ProduceWarps(warpsFac1toFac2);

            foreach (var te in TreatyEffects.Where(x => x.TreatyEffectType.IsOneTimeOnly == oneTimeOnly && x.TreatyEffectType.EffectGiveInfluence == true))
            {
                var org = te.Planet.PlanetFactions.FirstOrDefault(x => x.FactionID == te.GivingFactionID);
                if (org != null)
                {
                    var entry = te.Planet.PlanetFactions.FirstOrDefault(x => x.FactionID == te.ReceivingFactionID);
                    if (entry == null)
                    {
                        entry = new PlanetFaction() { PlanetID = te.Planet.PlanetID, FactionID = te.ReceivingFactionID };
                        te.Planet.PlanetFactions.Add(entry);
                    }
                    entry.Influence += org.Influence;
                    org.Influence = 0;
                }
            }

            return true;
        }

    }
}
