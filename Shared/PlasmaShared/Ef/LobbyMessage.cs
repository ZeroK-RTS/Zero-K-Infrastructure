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
    // LobbyMessage
    public partial class LobbyMessage
    {
        public int MessageID { get; set; } // MessageID (Primary key)
        public string SourceName { get; set; } // SourceName
        public string TargetName { get; set; } // TargetName
        public int? SourceLobbyID { get; set; } // SourceLobbyID
        public string Message { get; set; } // Message
        public DateTime Created { get; set; } // Created
        public int? TargetLobbyID { get; set; } // TargetLobbyID
        public string Channel { get; set; } // Channel
    }

}
