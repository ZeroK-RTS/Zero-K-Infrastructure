namespace ZkData
{
    /// <summary>
    /// The half of <c>GlobalConst</c> that cannot be linked here.
    ///
    /// Production resolves <c>Mode</c> at startup from settings or the compiled
    /// environment, in the same file that holds the content-service factory - so
    /// GlobalConst.cs itself needs WCF and cannot compile on .NET 9. This harness builds a
    /// schema, which does not depend on the deployment mode, so Local is stated explicitly
    /// rather than inherited.
    ///
    /// When the port reaches GlobalConst itself, this goes.
    /// </summary>
    public static partial class GlobalConst
    {
        public static ModeType Mode => ModeType.Local;

        /// <summary>
        /// What <c>SetMode(ModeType.Local)</c> assigns in GlobalConst.cs, stated here because
        /// that method cannot compile on .NET 9. It is a duplicated literal and can drift; it is
        /// here rather than in GlobalConst.Portable.cs so that it sits beside the Mode it belongs
        /// to, and both disappear together when the port reaches GlobalConst itself.
        /// </summary>
        public static string BaseSiteUrl => "https://localhost:44301";
    }
}
