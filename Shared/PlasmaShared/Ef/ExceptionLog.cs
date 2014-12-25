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
    // ExceptionLog
    public partial class ExceptionLog
    {
        public int ExceptionLogID { get; set; } // ExceptionLogID (Primary key)
        public int ProgramID { get; set; } // ProgramID
        public string Exception { get; set; } // Exception
        public string ExtraData { get; set; } // ExtraData
        public string RemoteIP { get; set; } // RemoteIP
        public string PlayerName { get; set; } // PlayerName
        public DateTime Time { get; set; } // Time
        public string ProgramVersion { get; set; } // ProgramVersion
        public string ExceptionHash { get; set; } // ExceptionHash
    }

}
