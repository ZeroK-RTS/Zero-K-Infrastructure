namespace ZkData
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("LobbyMessage")]
    public partial class LobbyMessage
    {
        [Key]
        public int MessageID { get; set; }

        [Required]
        [StringLength(200)]
        public string SourceName { get; set; }

        [Required]
        [StringLength(200)]
        public string TargetName { get; set; }

        public int? SourceLobbyID { get; set; }

        [StringLength(2000)]
        public string Message { get; set; }

        public DateTime Created { get; set; }

        public int? TargetLobbyID { get; set; }

        [StringLength(100)]
        public string Channel { get; set; }
    }
}
