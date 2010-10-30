using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Ionic.Zip;

namespace CMissionLib
{
	public static class Extensions
	{
		public static void CopyTo(this Stream input, Stream destination)
		{
			int num;
			var buffer = new byte[4096];
			while ((num = input.Read(buffer, 0, buffer.Length)) != 0)
			{
				destination.Write(buffer, 0, num);
			}
		}

		public static byte[] ToArray(this Stream stream)
		{
			using (var memoryStream = new MemoryStream()) 
			{
				stream.CopyTo(memoryStream);
				return memoryStream.ToArray();
			}
		}

		public static void SafeAddDirectory(this ZipFile zip, string directoryPath)
		{
			directoryPath = Path.GetFullPath(directoryPath) + "\\";
			foreach (var filePath in Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories))
			{
				var pathInArchive = filePath.Replace(directoryPath, String.Empty);
				if (!File.Exists(filePath)) throw new Exception("File does not exist: " + pathInArchive);
				zip.SafeAddFile(filePath, Path.GetDirectoryName(pathInArchive));
			}
		}

		public static void SafeAddEntry(this ZipFile zip, string itemName, Stream entry)
		{
			if (zip[itemName] == null) zip.AddEntry(itemName, entry);
		}

		public static void SafeAddFile(this ZipFile zip, string fileName, string directoryPathInArchive)
		{
			if (!File.Exists(fileName)) throw new Exception("File does not exist: " + fileName);
			var fileNameInArchive = directoryPathInArchive + Path.GetFileName(fileName);
			if (zip[fileNameInArchive] == null) zip.AddFile(fileName, directoryPathInArchive);
		}

		public static void SafeAddEntry(this ZipFile zip, string itemName, string entry)
		{
			if (zip[itemName] == null) zip.AddEntry(itemName, entry);
		}

		public static void SafeAddEntry (this ZipFile zip, string itemName, byte[] entry)
		{
			if (zip[itemName] == null) zip.AddEntry(itemName, entry);
		}

		[DllImport("gdi32.dll")]
		static extern bool DeleteObject(IntPtr hObject);

		public static BitmapSource ToBitmapSource(this Bitmap bitmap)
		{
			var hBitmap = bitmap.GetHbitmap();
			BitmapSource bitmapSource;
			try
			{
				bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero,
				                                                     Int32Rect.Empty,
				                                                     BitmapSizeOptions.FromEmptyOptions());
				bitmapSource.Freeze();
			}
			finally
			{
				DeleteObject(hBitmap);
			}
			return bitmapSource;
		}

		/// <summary>
		/// /// creates an image from a memory lump
		/// </summary>
		public static BitmapImage ToImage(this byte[] buffer)
		{
			using (var stream = new MemoryStream(buffer))
			{
				var image = new BitmapImage();
				image.BeginInit();
				image.CacheOption = BitmapCacheOption.OnLoad;
				image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
				image.StreamSource = stream;
				image.EndInit();
				image.Freeze();
				return image;
			}
		}

		/// <summary>
		/// /// creates an image from a memory lump
		/// </summary>
		public static BitmapImage ToImage(this byte[] buffer, int width, int height)
		{
			using (var stream = new MemoryStream(buffer))
			{
				var image = new BitmapImage();
				image.BeginInit();
				image.DecodePixelWidth = width;
				image.DecodePixelHeight = height;
				image.CacheOption = BitmapCacheOption.OnLoad;
				image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
				image.StreamSource = stream;
				image.EndInit();
				image.Freeze();
				return image;
			}
		}

		public static void Pin(this Array array, Action<IntPtr> action)
		{
			var handle = GCHandle.Alloc(array, GCHandleType.Pinned);
			try
			{
				action(handle.AddrOfPinnedObject());
			}
			finally
			{
				handle.Free();
			}
		}

		public static unsafe GCHandle Pin(this Array array, out byte* ptr)
		{
			var handle = GCHandle.Alloc(array, GCHandleType.Pinned);
			ptr = (byte*) handle.AddrOfPinnedObject();
			return handle;
		}

		public static T Pin<T>(this Array array, Func<IntPtr, T> action)
		{
			var handle = GCHandle.Alloc(array, GCHandleType.Pinned);
			try
			{
				return action(handle.AddrOfPinnedObject());
			}
			finally
			{
				handle.Free();
			}
		}

		/// <summary>
		/// Initializes a struct from binary data
		/// </summary>
		public static T ToStruct<T>(this byte[] buffer)
		{
			return buffer.Pin(p => (T) Marshal.PtrToStructure(p, typeof (T)));
		}

		/// <summary>
		/// Initializes a struct from a stream (reads only the reqired bytes)
		/// </summary>
		public static T ReadStruct<T>(this Stream stream)
		{
			var buffer = new byte[Marshal.SizeOf(typeof (T))];
			stream.Read(buffer, 0, buffer.Length);
			return buffer.ToStruct<T>();
		}

		/// <summary>
		/// Initializes a struct from a binary reader (reads only the reqired bytes)
		/// </summary>
		public static T ReadStruct<T>(this BinaryReader reader)
		{
			return reader.ReadBytes(Marshal.SizeOf(typeof (T))).ToStruct<T>();
		}


		/// <summary>
		/// Reads a null-terminated string
		/// </summary>
		public static string ReadCString(this BinaryReader reader)
		{
			var chars = new List<char>();
			while (true)
			{
				var c = reader.ReadChar();
				if (c == '\0') return new String(chars.ToArray());
				chars.Add(c);
			}
		}


		/// <summary>
		/// Like ToDictionary except if two values have the same key, the newer value used used instead of throwing an exception
		/// </summary>
		public static Dictionary<TKey, TValue> SafeToDictionary<TSource, TKey, TValue>(this IEnumerable<TSource> source,
		                                                                               Func<TSource, TKey> keySelector,
		                                                                               Func<TSource, TValue> valueSelector)
		{
			var dict = new Dictionary<TKey, TValue>();
			foreach (var item in source)
			{
				var key = keySelector(item);
				var value = valueSelector(item);
				dict.Remove(key);
				dict[key] = value;
			}
			return dict;
		}
	}
}