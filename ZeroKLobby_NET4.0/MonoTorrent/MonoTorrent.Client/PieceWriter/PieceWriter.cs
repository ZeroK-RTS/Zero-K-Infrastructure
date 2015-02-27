using System;
using MonoTorrent.Common;

namespace MonoTorrent.Client.PieceWriters
{
	public abstract class PieceWriter: IDisposable
	{
		protected PieceWriter() {}
		public virtual void Dispose() {}

		public abstract void Close(string path, TorrentFile file);

		internal void Close(string path, TorrentFile[] files)
		{
			Check.Path(path);
			Check.Files(files);
			foreach (var file in files) Close(path, file);
		}

		public abstract bool Exists(string path, TorrentFile file);

		internal bool Exists(string path, TorrentFile[] files)
		{
			Check.Path(path);
			Check.Files(files);
			foreach (var file in files) if (Exists(path, file)) return true;
			return false;
		}

		public abstract void Flush(string path, TorrentFile file);

		internal void Flush(string path, TorrentFile[] files)
		{
			Check.Path(path);
			Check.Files(files);
			foreach (var file in files) Flush(path, file);
		}

		public abstract void Move(string oldPath, string newPath, TorrentFile file, bool ignoreExisting);

		internal void Move(string oldPath, string newPath, TorrentFile[] files, bool ignoreExisting)
		{
			foreach (var file in files) Move(oldPath, newPath, file, ignoreExisting);
		}

		public abstract int Read(BufferedIO data);

		internal int ReadChunk(BufferedIO data)
		{
			// Copy the inital buffer, offset and count so the values won't
			// be lost when doing the reading.
			var orig = data.buffer;
			var origOffset = data.Offset;
			var origCount = data.Count;

			var read = 0;
			var totalRead = 0;

			// Read the data in chunks. For every chunk we read,
			// advance the offset and subtract from the count. This
			// way we can keep filling in the buffer correctly.
			while (totalRead != data.Count)
			{
				read = Read(data);
				data.buffer = new ArraySegment<byte>(data.buffer.Array, data.buffer.Offset + read, data.buffer.Count - read);
				data.Offset += read;
				data.Count -= read;
				totalRead += read;

				if (read == 0 || data.Count == 0) break;
			}

			// Restore the original values so the object remains unchanged
			// as compared to when the user passed it in.
			data.buffer = orig;
			data.Offset = origOffset;
			data.Count = origCount;
			data.ActualCount = totalRead;
			return totalRead;
		}

		public abstract void Write(BufferedIO data);
	}
}