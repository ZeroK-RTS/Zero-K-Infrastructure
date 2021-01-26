using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PlasmaShared;

namespace ZkData
{
    public class GameMode
    {
        public int GameModeID { get; set; }
         
        public bool IsFeatured { get; set; }
        
        [Index(IsUnique = true)]
        [MaxLength(64)]
        public string ShortName { get; set; }
        
        public string DisplayName { get; set; }
        
       
        public DateTime Created { get; set; }
        
        public DateTime LastModified { get; set; }

        public int? ForumThreadID { get; set; }
        
        [ForeignKey(nameof(ForumThreadID))]
        public virtual ForumThread ForumThread { get; set; }
        
        public int MaintainerAccountID { get; set; }
        
        [ForeignKey(nameof(MaintainerAccountID))]
        public virtual Account Maintainer { get; set; }

        
        public string GameModeJson { get; set; }
        
    }
}