namespace ZkData
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("MiscVar")]
    public partial class MiscVar
    {
        [Key]
        public string VarName { get; set; }

        public string VarValue { get; set; }
    }
}
