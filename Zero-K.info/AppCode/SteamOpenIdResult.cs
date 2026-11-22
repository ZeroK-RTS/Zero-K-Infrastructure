namespace ZeroKWeb
{
    /// <summary>
    /// What a Steam sign-in attempt came back as. One source, linked into the .NET 9 build, so
    /// the two implementations of the exchange cannot disagree about their own result type.
    /// </summary>
    public enum SteamOpenIdStatus
    {
        Authenticated,
        Canceled,
        Failed,
    }

    /// <summary>
    /// The outcome of a return from Steam. <see cref="SteamID"/> is meaningful only when
    /// <see cref="Status"/> is <see cref="SteamOpenIdStatus.Authenticated"/>.
    /// </summary>
    public class SteamOpenIdResult
    {
        public SteamOpenIdStatus Status;
        public ulong SteamID;
        public string Referer;

        /// <summary>Why it failed, for the trace log. Never shown to the visitor.</summary>
        public string FailureReason;
    }
}
