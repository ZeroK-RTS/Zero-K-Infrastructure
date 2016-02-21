using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace ZeroKLobby
{
    public class CachedResourceManager : ResourceManager
    {
        protected CachedResourceManager() {}

        Dictionary<string, object> cachedObjects = new Dictionary<string, object>();
        public CachedResourceManager([NotNull] string baseName, [NotNull] Assembly assembly): base(baseName, assembly) {}

        public CachedResourceManager([NotNull] string baseName, [NotNull] Assembly assembly, Type usingResourceSet): base(baseName, assembly, usingResourceSet) {}

        public CachedResourceManager([NotNull] Type resourceSource): base(resourceSource) {}

        public override object GetObject(string name)
        {
            object obj;
            if (cachedObjects.TryGetValue(name, out obj)) return obj;
            obj = base.GetObject(name);
            cachedObjects[name] = obj;
            return obj;
        }

        public override object GetObject(string name, CultureInfo culture)
        {
            object obj;
            if (cachedObjects.TryGetValue(name, out obj)) return obj;
            obj = base.GetObject(name, culture);
            cachedObjects[name] = obj;
            return obj;
        }
    }
}
