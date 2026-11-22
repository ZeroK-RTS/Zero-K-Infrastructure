using System;

namespace ZkData
{
    /// <summary>
    /// EF6's <c>DbContext.Configuration</c>, as far as production code uses it - which is three
    /// lines in MissionService.svc.cs, all of them <c>ProxyCreationEnabled = false</c>.
    /// </summary>
    public partial class ZkDataContext
    {
        public DbContextConfigurationCompat Configuration { get; } = new DbContextConfigurationCompat();
    }

    /// <summary>
    /// **Accepts only the answer that is already true here.**
    ///
    /// EF6 generated dynamic proxy subclasses for lazy loading and change tracking, and turning
    /// them off was how a caller said "give me plain objects". EF Core creates no proxies unless
    /// UseLazyLoadingProxies is called, and this context does not call it - so setting false asks
    /// for what is already the case, and the port can honour it exactly.
    ///
    /// Setting TRUE is refused rather than ignored. A caller who asks for proxies wants lazy
    /// loading, and silently not providing it is how this port has already been bitten: navigation
    /// properties that came back null looked like missing data rather than a missing feature.
    /// Throwing says which line to look at.
    /// </summary>
    public class DbContextConfigurationCompat
    {
        private bool proxyCreationEnabled;

        public bool ProxyCreationEnabled
        {
            get { return proxyCreationEnabled; }
            set
            {
                if (value)
                    throw new NotSupportedException(
                        "ProxyCreationEnabled = true is EF6 lazy-loading proxies, which this context does not " +
                        "configure. EF Core would return null navigations instead of loading them, so this is " +
                        "refused rather than ignored - use Include() for what the query needs.");
                proxyCreationEnabled = false;
            }
        }
    }
}
