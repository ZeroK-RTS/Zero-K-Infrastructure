using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace ZkData
{
    public class FactionTreaty
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

        public int? ProposingFactionGuarantee { get; set; }
        public int? AcceptingFactionGuarantee { get; set; }

        public TreatyState TreatyState { get; set; }

        public TreatyUnableToTradeMode TreatyUnableToTradeMode { get; set; }


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
            if (ProposingFactionID == account.FactionID || AcceptingFactionID == account.FactionID) return true; // can canel
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

        public bool StoreGuarantee()
        {
            var propMetal = ProposingFactionGuarantee ?? 0;
            var acceptMetal = AcceptingFactionGuarantee ?? 0;
            if (FactionByProposingFactionID.Metal >= propMetal && FactionByAcceptingFactionID.Metal >= acceptMetal)
            {
                FactionByProposingFactionID.SpendMetal(propMetal);
                FactionByAcceptingFactionID.SpendMetal(acceptMetal);
                return true;
            }

            return false;
        }

        public void CancelTreaty(Faction faction)
        {
            bool wasAccepted = TreatyState == TreatyState.Accepted;
            TreatyState = TreatyState.Cancelled;

            if (wasAccepted)
            {
                if (faction?.FactionID == AcceptingFactionID)
                {
                    FactionByProposingFactionID.ProduceMetal(AcceptingFactionGuarantee ?? 0);
                    FactionByProposingFactionID.ProduceMetal(ProposingFactionGuarantee ?? 0);
                }
                else
                {
                    FactionByAcceptingFactionID.ProduceMetal(AcceptingFactionGuarantee ?? 0);
                    FactionByAcceptingFactionID.ProduceMetal(ProposingFactionGuarantee ?? 0);
                }
            }
        }



        public Faction ProcessTrade(bool oneTimeOnly)
        {
            var fac1 = FactionByProposingFactionID;
            var fac2 = FactionByAcceptingFactionID;

            double metalFac1toFac2 = 0;
            double energyFac1toFac2 = 0;
            double dropshipsFac1toFac2 = 0;
            double bombersFac1toFac2 = 0;
            double warpsFac1toFac2 = 0;
            double victoryPointsFac1toFac2 = 0;


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

                if (tr.EffectGiveVictoryPoints == true)
                {
                    if (fac1 == te.FactionByGivingFactionID) victoryPointsFac1toFac2 += te.Value ?? 0;
                    else victoryPointsFac1toFac2 -= te.Value ?? 0;
                }

            }

            if (fac1.Metal < metalFac1toFac2) return fac1;
            if (fac2.Metal < -metalFac1toFac2) return fac2;
            if (fac1.EnergyProducedLastTurn < energyFac1toFac2) return fac1;
            if (fac2.EnergyProducedLastTurn < -energyFac1toFac2) return fac2;
            if (fac1.Dropships < dropshipsFac1toFac2) return fac1;
            if (fac2.Dropships < -dropshipsFac1toFac2) return fac2;
            if (fac1.Bombers < bombersFac1toFac2) return fac1;
            if (fac2.Bombers < -bombersFac1toFac2) return fac2;
            if (fac1.Warps < warpsFac1toFac2) return fac1;
            if (fac2.Warps < -warpsFac1toFac2) return fac2;
            if (fac1.VictoryPoints < victoryPointsFac1toFac2) return fac1;
            if (fac2.VictoryPoints < -victoryPointsFac1toFac2) return fac2;


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
            fac1.VictoryPoints -= victoryPointsFac1toFac2;
            fac2.VictoryPoints += victoryPointsFac1toFac2;


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
                    double transfer = Math.Min(te.Value ?? 0, org.Influence);
                    entry.Influence += transfer;
                    org.Influence -= transfer;
                }
            }

            return null;
        }

    }
}
