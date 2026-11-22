namespace ZkData
{
    /// <summary>
    /// The half of <c>GlobalConst</c> that cannot be linked into this project.
    ///
    /// Production's <c>GlobalConst.Mode</c> is resolved at startup from settings or the
    /// compiled environment, in the same file that holds the content-service factory - so
    /// GlobalConst.cs itself does not compile outside .NET Framework. Tests run as Local,
    /// stated here explicitly rather than inherited from an ambient environment.
    ///
    /// Anything that reads <c>Mode</c> and is under test must therefore be exercised for
    /// the Live case deliberately; see RatingConstantsTests.
    /// </summary>
    public static partial class GlobalConst
    {
        public static ModeType Mode => ModeType.Local;
    }
}
