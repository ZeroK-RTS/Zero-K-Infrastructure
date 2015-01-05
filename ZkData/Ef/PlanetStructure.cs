using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;

namespace ZkData
{
    public partial class PlanetStructure
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
        public int? ActivatedOnTurn { get; set; }
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
                if (ActivatedOnTurn == null) ActivatedOnTurn = turn;
                if (StructureType.TurnsToActivate == null) IsActive = true;
                else if (ActivatedOnTurn != null && ActivatedOnTurn.Value + StructureType.TurnsToActivate.Value <= turn) IsActive = true;

            }

        }


        public void UnpoweredTick(int turn)
        {
            if ((StructureType.UpkeepEnergy ?? 0) > 0)
            {
                IsActive = false;
                if (ActivatedOnTurn <= turn) ActivatedOnTurn = turn + 1;
            }

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

    }
}
