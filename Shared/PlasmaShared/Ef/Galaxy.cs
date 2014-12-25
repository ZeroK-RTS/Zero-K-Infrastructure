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
    // Galaxy
    public partial class Galaxy
    {
        public int GalaxyID { get; set; } // GalaxyID (Primary key)
        public DateTime? Started { get; set; } // Started
        public int Turn { get; set; } // Turn
        public DateTime? TurnStarted { get; set; } // TurnStarted
        public string ImageName { get; set; } // ImageName
        public bool IsDirty { get; set; } // IsDirty
        public int Width { get; set; } // Width
        public int Height { get; set; } // Height
        public bool IsDefault { get; set; } // IsDefault
        public int AttackerSideCounter { get; set; } // AttackerSideCounter
        public DateTime? AttackerSideChangeTime { get; set; } // AttackerSideChangeTime
        public string MatchMakerState { get; set; } // MatchMakerState

        // Reverse navigation
        public virtual ICollection<Link> Links { get; set; } // Link.FK_Link_Galaxy
        public virtual ICollection<Planet> Planets { get; set; } // Planet.FK_Planet_Galaxy

        public Galaxy()
        {
            ImageName = "N'galaxy1.jpg'";
            IsDirty = true;
            Width = 100;
            Height = 100;
            IsDefault = false;
            AttackerSideCounter = 0;
            Links = new List<Link>();
            Planets = new List<Planet>();
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
