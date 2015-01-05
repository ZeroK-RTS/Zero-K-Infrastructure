using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ZkData
{
    public partial class RoleType
    {
        
        public RoleType()
        {
            AccountRoles = new HashSet<AccountRole>();
            Polls = new HashSet<Poll>();
            RoleTypeHierarchiesByMasterRoleTypeID = new HashSet<RoleTypeHierarchy>();
            RoleTypeHierarchiesBySlaveRoleTypeID = new HashSet<RoleTypeHierarchy>();
        }

        public int RoleTypeID { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; }

        [Required]
        [StringLength(500)]
        public string Description { get; set; }

        public bool IsClanOnly { get; set; }

        public bool IsOnePersonOnly { get; set; }

        public int? RestrictFactionID { get; set; }

        public bool IsVoteable { get; set; }

        public int PollDurationDays { get; set; }

        public double RightDropshipQuota { get; set; }

        public double RightBomberQuota { get; set; }

        public double RightMetalQuota { get; set; }

        public double RightWarpQuota { get; set; }

        public bool RightDiplomacy { get; set; }

        public bool RightEditTexts { get; set; }

        public bool RightSetEnergyPriority { get; set; }

        public bool RightKickPeople { get; set; }

        public int DisplayOrder { get; set; }

        
        public virtual ICollection<AccountRole> AccountRoles { get; set; }

        public virtual Faction Faction { get; set; }

        
        public virtual ICollection<Poll> Polls { get; set; }

        
        public virtual ICollection<RoleTypeHierarchy> RoleTypeHierarchiesByMasterRoleTypeID { get; set; }

        
        public virtual ICollection<RoleTypeHierarchy> RoleTypeHierarchiesBySlaveRoleTypeID { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
