using System.ComponentModel.DataAnnotations.Schema;

namespace ZkData
{
	partial class News
	{
        [NotMapped]
		public string ImageRelativeUrl {
			get { if (ImageExtension == null) return null; return string.Format("/img/news/{0}{1}", NewsID, ImageExtension); }
		}

        [NotMapped]
        public string ThumbRelativeUrl
        {
            get { if (ImageExtension == null) return null; return string.Format("/img/news/{0}_thumb{1}", NewsID, ImageExtension); }
        }
	}
}
