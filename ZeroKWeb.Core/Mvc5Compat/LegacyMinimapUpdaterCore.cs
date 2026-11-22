namespace ZeroKWeb
{
    /// <summary>
    /// The .NET 9 half of the lazy minimap-correction twin pair; the MVC 5 one is
    /// Zero-K.info/AppCode/LegacyMinimapUpdater.cs and it re-registers the map.
    ///
    /// **This one does nothing, and that is the whole design rather than a gap.** Correcting a
    /// map's images by re-registering it runs unitsync, the native Spring library, through
    /// AutoRegistrator - and on .NET 9 that is AutoRegistratorCompat, a tripwire that throws on its
    /// first line because none of it is portable by writing C#. So there is nothing here for this
    /// half to call, and a no-op is more honest than a call that would throw on the first map
    /// anybody opened.
    ///
    /// The pair exists because MapsController is LINKED - the same file compiles in both stacks -
    /// so the name it calls has to resolve on both. Same shape as MapImage/MapImageCore and
    /// AuthCookieCompat: one name, two implementations, and the build catches any drift in the
    /// signature because a linked caller has to compile against both.
    ///
    /// When the registrar does run somewhere reachable from the ported site, this is the file that
    /// changes - and the MVC 5 half is the specification for what it has to do.
    /// </summary>
    public static class LegacyMinimapUpdater
    {
        /// <summary>
        /// Takes the map's name and ignores it. Deliberately not even a Trace line: it would be
        /// written for every map view on the ported site, saying the same thing every time.
        /// </summary>
        public static void Notice(string internalName)
        {
        }
    }
}
