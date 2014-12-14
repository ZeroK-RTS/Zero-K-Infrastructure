using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ZkData
{
	partial class News
	{
		public string ImageRelativeUrl {
			get { if (ImageExtension == null) return null; return string.Format("/img/news/{0}{1}", NewsID, ImageExtension); }
		}
        public string ThumbRelativeUrl
        {
            get { if (ImageExtension == null) return null; return string.Format("/img/news/{0}{1}_thumb", NewsID, ImageExtension); }
        }
	}
}
