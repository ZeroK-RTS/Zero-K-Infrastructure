namespace ZkData
{
    /// <summary>
    /// Deployment environment. Pure enum, kept in its own file so it can be linked into
    /// the .NET 9 test project - see Tests.Portable/Tests.Portable.csproj.
    /// </summary>
    public enum ModeType
    {
        Local = 0, // localhost debugging
        Test = 1, // test.zero-k.info
        Live = 2, // LIVE 
    }
}
