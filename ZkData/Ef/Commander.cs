using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ZkData
{
    public partial class Commander
    {
        
        public Commander()
        {
            CommanderDecorations = new HashSet<CommanderDecoration>();
            CommanderModules = new HashSet<CommanderModule>();
        }

        public int CommanderID { get; set; }

        public int AccountID { get; set; }

        public int ProfileNumber { get; set; }

        [StringLength(200)]
        public string Name { get; set; }

        public int ChassisUnlockID { get; set; }

        public virtual Account AccountByAccountID { get; set; }

        public virtual Unlock Unlock { get; set; }

        
        public virtual ICollection<CommanderDecoration> CommanderDecorations { get; set; }

        
        public virtual ICollection<CommanderModule> CommanderModules { get; set; }
    }
}
