using System.ComponentModel;

namespace ZkData
{
    public enum TreatyState
    {
        Invalid = 0,
        Proposed = 1,
        Accepted = 2,
        Suspended = 3,
    }

    public enum TreatyUnableToTradeMode
    {
        [Description("cancel and pay guarantee")]
        Cancel = 0,
        [Description("suspend effects turn counter")]
        Suspend = 1
    }

}
