using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using PlasmaShared;

namespace ZkData
{
    public class PlanetStructure
    {
        [Key]
        [Column(Order = 0)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int PlanetID { get; set; }
        [Key]
        [Column(Order = 1)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int StructureTypeID { get; set; }
        public int? OwnerAccountID { get; set; }

        public int? ActivationTurnCounter { get; set; }
        public int? TurnsToActivateOverride { get; set; }

        public EnergyPriority EnergyPriority { get; set; }
        public bool IsActive { get; set; }
        public int? TargetPlanetID { get; set; }
        public virtual Account Account { get; set; }
        public virtual Planet Planet { get; set; }
        public virtual Planet PlanetByTargetPlanetID { get; set; }
        public virtual StructureType StructureType { get; set; }

        public void PoweredTick(int turn)
        {
            if (IsActive) return;
            if (!IsActive)
            {
                if (ActivationTurnCounter == null) ActivationTurnCounter = 1;
                else ActivationTurnCounter++;

                var turnsNeeded = TurnsToActivateOverride ?? StructureType.TurnsToActivate;

                if (turnsNeeded == null || turnsNeeded <= ActivationTurnCounter) IsActive = true;
            }

        }


        public void UnpoweredTick(int turn)
        {
            if ((StructureType.UpkeepEnergy ?? 0) > 0)
            {
                if (IsActive) TurnsToActivateOverride = null;
                IsActive = false;
                ActivationTurnCounter = null;
            }

        }


        public void ReactivateAfterDestruction()
        {
            IsActive = false;
            ActivationTurnCounter = null;
            TurnsToActivateOverride = StructureType.TurnsToReactivate;
        }

        public void ReactivateAfterBuild()
        {
            IsActive = false;
            ActivationTurnCounter = null;
            TurnsToActivateOverride = StructureType.TurnsToActivate;
        }


        public string GetImageUrl()
        {
            if (!IsActive) return string.Format((string)"/img/structures/{0}", StructureType.DisabledMapIcon);
            else return string.Format((string)"/img/structures/{0}", StructureType.MapIcon);
        }

        public string GetPathResized(int size)
        {
            return "/img/structures/" + GetFileNameResized(size);
        }

        public string GetFileNameResized(int size)
        {
            var fileName = !IsActive ? StructureType.DisabledMapIcon : StructureType.MapIcon;
            var extension = Path.GetExtension(fileName);
            return String.Format("{0}_r_{1}{2}", Path.GetFileNameWithoutExtension(fileName), size, extension);
        }


        public void GenerateResized(int size, string folder)
        {
            GenerateResized(size, folder, true);
            GenerateResized(size, folder, false);
        }



        public void GenerateResized(int size, string folder, bool destroyed)
        {
            using (var image = Image.FromFile(folder + "/" + (!IsActive ? StructureType.DisabledMapIcon : StructureType.MapIcon)))
            {
                using (var resized = image.GetResized(size, size, InterpolationMode.HighQualityBilinear))
                {
                    var fileNameResized = GetFileNameResized(size);
                    resized.Save(folder + "/" + fileNameResized);
                }
            }
        }


        public bool IsRushed()
        {
            return IsActive || TurnsToActivateOverride <= (StructureType.RushActivationTime ?? 0);
        }

        public bool CanRush(Account account)
        {
            if (account == null) return false;
            if (!IsRushed() && Planet.OwnerFactionID == account.FactionID && Planet.OwnerFactionID != null &&
                StructureType.MetalToRushActivation > 0 && account.GetMetalAvailable() >= StructureType.MetalToRushActivation) return true;

            else return false;
        }

        public bool RushStructure(Account account)
        {
            if (!CanRush(account)) return false;
            TurnsToActivateOverride = StructureType.RushActivationTime ?? 0;
            ActivationTurnCounter = null;
            if (TurnsToActivateOverride == 0) IsActive = true;
            account.SpendMetal(StructureType.MetalToRushActivation ?? 0);
            return true;
        }

    }
}
