using System;
using MonoTorrent.BEncoding;

namespace MonoTorrent.Common
{
	public class TorrentCreatorAsyncResult: AsyncResult
	{
		bool aborted;

		public bool Aborted { get { return aborted; } }

		internal BEncodedDictionary Dictionary { get; set; }

		public TorrentCreatorAsyncResult(AsyncCallback callback, object asyncState): base(callback, asyncState) {}

		public void Abort()
		{
			aborted = true;
		}
	}
}