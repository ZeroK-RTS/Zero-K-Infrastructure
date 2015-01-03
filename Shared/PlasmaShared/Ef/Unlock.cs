namespace ZkData
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("Unlock")]
    public partial class Unlock
    {
        
        public Unlock()
        {
            AccountUnlocks = new HashSet<AccountUnlock>();
            Commanders = new HashSet<Commander>();
            CommanderDecorations = new HashSet<CommanderDecoration>();
            CommanderModules = new HashSet<CommanderModule>();
            KudosPurchases = new HashSet<KudosPurchase>();
            StructureTypes = new HashSet<StructureType>();
            ChildUnlocks = new HashSet<Unlock>();
        }

        public int UnlockID { get; set; }

        [Required]
        [StringLength(100)]
        public string Code { get; set; }

        [StringLength(200)]
        public string Name { get; set; }

        [StringLength(1000)]
        public string Description { get; set; }

        public int NeededLevel { get; set; }

        [StringLength(500)]
        public string LimitForChassis { get; set; }

        public UnlockTypes UnlockType { get; set; }

        public int? RequiredUnlockID { get; set; }

        public int MorphLevel { get; set; }

        public int MaxModuleCount { get; set; }

        public int? MetalCost { get; set; }

        public int XpCost { get; set; }

        public int? MetalCostMorph2 { get; set; }

        public int? MetalCostMorph3 { get; set; }

        public int? MetalCostMorph4 { get; set; }

        public int? MetalCostMorph5 { get; set; }

        public int? KudosCost { get; set; }

        public bool? IsKudosOnly { get; set; }

        
        public virtual ICollection<AccountUnlock> AccountUnlocks { get; set; }

        
        public virtual ICollection<Commander> Commanders { get; set; }

        
        public virtual ICollection<CommanderDecoration> CommanderDecorations { get; set; }

        public virtual CommanderDecorationIcon CommanderDecorationIcon { get; set; }

        
        public virtual ICollection<CommanderModule> CommanderModules { get; set; }

        
        public virtual ICollection<KudosPurchase> KudosPurchases { get; set; }

        
        public virtual ICollection<StructureType> StructureTypes { get; set; }

        
        public virtual ICollection<Unlock> ChildUnlocks { get; set; }

        public virtual Unlock ParentUnlock { get; set; }
    }
}
