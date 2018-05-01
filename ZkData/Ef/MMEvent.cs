using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ZkData
{
    public class MMEvent
    {
        public MMEvent()
        {
            SpringBattles = new HashSet<SpringBattle>();
        }
        public int EventID { get; set; }
        [Required]
        [StringLength(4000)]
        public string Text { get; set; }
        public DateTime Time { get; set; }
        [StringLength(4000)]
        public string PlainText { get; set; }
        public virtual ICollection<SpringBattle> SpringBattles { get; set; }

    }
}
