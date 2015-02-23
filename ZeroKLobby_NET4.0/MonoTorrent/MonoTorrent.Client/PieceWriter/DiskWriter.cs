using System;
using System.Collections.Generic;
using System.IO;
using MonoTorrent.Common;

namespace MonoTorrent.Client.PieceWriters
{
	public class DiskWriter: PieceWriter
	{
		Dictionary<TorrentFile, string> paths;
		readonly FileStreamBuffer streamsBuffer;

		public int OpenFiles { get { return streamsBuffer.Count; } }

		public DiskWriter(): this(10) {}

		public DiskWriter(int maxOpenFiles)
		{
			paths = new Dictionary<TorrentFile, string>();
			streamsBuffer = new FileStreamBuffer(maxOpenFiles);
		}

		public override void Dispose()
		{
			streamsBuffer.Dispose();
			base.Dispose();
		}

		internal TorrentFileStream GetStream(string path, TorrentFile file, FileAccess access)
		{
			path = GenerateFilePath(path, file);
			return streamsBuffer.GetStream(file, path, access);
		}

		public override void Close(string path, TorrentFile file)
		{
			streamsBuffer.CloseStream(GenerateFilePath(path, file));
		}

		public override bool Exists(string path, TorrentFile file)
		{
			return File.Exists(GenerateFilePath(path, file));
		}

		public override void Flush(string path, TorrentFile file)
		{
			Stream s = streamsBuffer.FindStream(GenerateFilePath(path, file));
			if (s != null) s.Flush();
		}

		public override void Move(string oldPath, string newPath, TorrentFile file, bool ignoreExisting)
		{
			var oldFile = GenerateFilePath(oldPath, file);
			var newFile = GenerateFilePath(newPath, file);
			streamsBuffer.CloseStream(oldFile);
			if (ignoreExisting) File.Delete(newFile);
			File.Move(oldFile, newFile);
		}

		public override int Read(BufferedIO data)
		{
			if (data == null) throw new ArgumentNullException("buffer");

			var offset = data.Offset;
			var count = data.Count;
			var files = data.Files;
			var fileSize = Toolbox.Accumulate<TorrentFile>(files, delegate(TorrentFile f) { return f.Length; });
			if (offset < 0 || offset + count > fileSize) throw new ArgumentOutOfRangeException("offset");

			var i = 0;
			var bytesRead = 0;
			var totalRead = 0;

			for (i = 0; i < files.Length; i++) // This section loops through all the available
			{
				// files until we find the file which contains
				if (offset < files[i].Length) // the start of the data we want to read
					break;

				offset -= files[i].Length; // Offset now contains the index of the data we want
			} // to read from fileStream[i].

			while (totalRead < count) // We keep reading until we have read 'count' bytes.
			{
				if (i == files.Length) break;

				var s = GetStream(data.Path, files[i], FileAccess.Read);
				s.Seek(offset, SeekOrigin.Begin);
				offset = 0; // Any further files need to be read from the beginning
				bytesRead = s.Read(data.buffer.Array, data.buffer.Offset + totalRead, count - totalRead);
				totalRead += bytesRead;
				i++;
			}
			//monitor.BytesSent(totalRead, TransferType.Data);
			data.ActualCount += totalRead;
			return totalRead;
		}

		public override void Write(BufferedIO data)
		{
			var buffer = data.buffer.Array;
			var offset = data.Offset;
			var count = data.Count;

			if (buffer == null) throw new ArgumentNullException("buffer");

			long fileSize = 0;
			for (var j = 0; j < data.Files.Length; j++) fileSize += data.Files[j].Length;

			if (offset < 0 || offset + count > fileSize) throw new ArgumentOutOfRangeException("offset");

			var i = 0;
			long bytesWritten = 0;
			long totalWritten = 0;
			long bytesWeCanWrite = 0;

			for (i = 0; i < data.Files.Length; i++) // This section loops through all the available
			{
				// files until we find the file which contains
				if (offset < data.Files[i].Length) // the start of the data we want to write
					break;

				offset -= data.Files[i].Length; // Offset now contains the index of the data we want
			} // to write to fileStream[i].

			while (totalWritten < count) // We keep writing  until we have written 'count' bytes.
			{
				var stream = GetStream(data.Path, data.Files[i], FileAccess.ReadWrite);
				stream.Seek(offset, SeekOrigin.Begin);

				// Find the maximum number of bytes we can write before we reach the end of the file
				bytesWeCanWrite = data.Files[i].Length - offset;

				// Any further files need to be written from the beginning of the file
				offset = 0;

				// If the amount of data we are going to write is larger than the amount we can write, just write the allowed
				// amount and let the rest of the data be written with the next filestream
				bytesWritten = ((count - totalWritten) > bytesWeCanWrite) ? bytesWeCanWrite : (count - totalWritten);

				// Write the data
				stream.Write(buffer, data.buffer.Offset + (int)totalWritten, (int)bytesWritten);

				// Any further data should be written to the next available file
				totalWritten += bytesWritten;
				i++;
			}
			ClientEngine.BufferManager.FreeBuffer(ref data.buffer);
			//monitor.BytesReceived((int)totalWritten, TransferType.Data);
		}

		protected virtual string GenerateFilePath(string path, TorrentFile file)
		{
			path = Path.Combine(path, file.Path);
			var directory = Path.GetDirectoryName(path);

			if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory)) Directory.CreateDirectory(directory);

			return path;
		}
	}
}