using System.Collections.Concurrent;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace PlasmaShared
{
    public class ResizedImageCache
    {
        public class CacheKey
        {
            public Image Image;
            public int Width;
            public int Height;
            public CacheKey(Image image, int width, int height)
            {
                Image = image;
                Width = width;
                Height = height;
            }

            protected bool Equals(CacheKey other)
            {
                return Equals(Image, other.Image) && Width == other.Width && Height == other.Height;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((CacheKey)obj);
            }

            public override int GetHashCode()
            {
                unchecked {
                    int hashCode = (Image != null ? Image.GetHashCode() : 0);
                    hashCode = (hashCode*397) ^ Width;
                    hashCode = (hashCode*397) ^ Height;
                    return hashCode;
                }
            }
        }
        ConcurrentDictionary<CacheKey,Image> cachedImages=  new ConcurrentDictionary<CacheKey, Image>();

        public static readonly ResizedImageCache Instance = new ResizedImageCache();

        public Image GetResizedWithCache(Image source, int width, int height, InterpolationMode mode = InterpolationMode.HighQualityBicubic)
        {
            var key = new CacheKey(source, width, height);
            return cachedImages.GetOrAdd(key, (k) => {
                var resized = new Bitmap(k.Width, k.Height);
                using (var g = Graphics.FromImage(resized)) {
                    g.InterpolationMode = mode;
                    g.DrawImage(source, 0, 0, k.Width, k.Height);    
                }
                return resized;
            });
        }

    }
}