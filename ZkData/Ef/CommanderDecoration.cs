using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZkData
{
    public partial class CommanderDecoration
    {
        [Key]
        [Column(Order = 0)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int CommanderID { get; set; }

        public int DecorationUnlockID { get; set; }

        [Key]
        [Column(Order = 1)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int SlotID { get; set; }

        public virtual Commander Commander { get; set; }

        public virtual CommanderDecorationSlot CommanderDecorationSlot { get; set; }

        public virtual Unlock Unlock { get; set; }

        static string[] iconPositions = { "overhead", "back", "chest", "shoulders" };

        public static string GetIconPosition(CommanderDecorationIcon decoration)
        {
            int num = decoration.IconPosition;
            return iconPositions[num];
        }

        public static string GetIconPosition(Unlock decoration)
        {
            return GetIconPosition(decoration.CommanderDecorationIcon);
        }

        public static string GetIconPosition(CommanderDecoration decoration)
        {
            return GetIconPosition(decoration.Unlock.CommanderDecorationIcon);
        }

    }
}
