// ReSharper disable RedundantUsingDirective
// ReSharper disable DoNotCallOverridableMethodsInConstructor
// ReSharper disable InconsistentNaming
// ReSharper disable PartialTypeWithSinglePart
// ReSharper disable PartialMethodWithSinglePart
// ReSharper disable RedundantNameQualifier

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration;
//using DatabaseGeneratedOption = System.ComponentModel.DataAnnotations.DatabaseGeneratedOption;

namespace ZkData
{
    // ResourceContentFile
    public partial class ResourceContentFile
    {
        public int ResourceID { get; set; } // ResourceID (Primary key)
        public string Md5 { get; set; } // Md5 (Primary key)
        public int Length { get; set; } // Length
        public string FileName { get; set; } // FileName
        public string Links { get; set; } // Links
        public int LinkCount { get; set; } // LinkCount

        // Foreign keys
        public virtual Resource Resource { get; set; } // FK_ResourceContentFile_Resource
    }

}
