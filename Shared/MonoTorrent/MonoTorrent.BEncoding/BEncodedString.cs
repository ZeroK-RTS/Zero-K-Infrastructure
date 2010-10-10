using System;
using System.Net;
using System.Text;
using MonoTorrent.Client.Messages;
using MonoTorrent.Common;
	//
	// Message.cs
	//
	// Authors:
	//   Alan McGovern alan.mcgovern@gmail.com
	//
	// Copyright (C) 2008 Alan McGovern
	//
	// Permission is hereby granted, free of charge, to any person obtaining
	// a copy of this software and associated documentation files (the
	// "Software"), to deal in the Software without restriction, including
	// without limitation the rights to use, copy, modify, merge, publish,
	// distribute, sublicense, and/or sell copies of the Software, and to
	// permit persons to whom the Software is furnished to do so, subject to
	// the following conditions:
	// 
	// The above copyright notice and this permission notice shall be
	// included in all copies or substantial portions of the Software.
	// 
	// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
	// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
	// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
	// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
	// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
	// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
	// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
	//

namespace MonoTorrent.Client.Messages
{
	public abstract class Message
	{
		public abstract int ByteLength { get; }

		public abstract void Decode(byte[] buffer, int offset, int length);

		public void Decode(ArraySegment<byte> buffer, int offset, int length)
		{
			Decode(buffer.Array, buffer.Offset + offset, length);
		}

		public byte[] Encode()
		{
			var buffer = new byte[ByteLength];
			Encode(buffer, 0);
			return buffer;
		}

		public abstract int Encode(byte[] buffer, int offset);

		public int Encode(ArraySegment<byte> buffer, int offset)
		{
			return Encode(buffer.Array, buffer.Offset + offset);
		}

		public static byte ReadByte(byte[] buffer, int offset)
		{
			return buffer[offset];
		}

		public static byte ReadByte(byte[] buffer, ref int offset)
		{
			var b = buffer[offset];
			offset++;
			return b;
		}

		public static byte[] ReadBytes(byte[] buffer, int offset, int count)
		{
			return ReadBytes(buffer, ref offset, count);
		}

		public static byte[] ReadBytes(byte[] buffer, ref int offset, int count)
		{
			var result = new byte[count];
			Buffer.BlockCopy(buffer, offset, result, 0, count);
			offset += count;
			return result;
		}

		public static int ReadInt(byte[] buffer, int offset)
		{
			return ReadInt(buffer, ref offset);
		}

		public static int ReadInt(byte[] buffer, ref int offset)
		{
			var ret = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buffer, offset));
			offset += 4;
			return ret;
		}

		public static long ReadLong(byte[] buffer, int offset)
		{
			return ReadLong(buffer, ref offset);
		}

		public static long ReadLong(byte[] buffer, ref int offset)
		{
			var ret = IPAddress.NetworkToHostOrder(BitConverter.ToInt64(buffer, offset));
			offset += 8;
			return ret;
		}

		public static short ReadShort(byte[] buffer, int offset)
		{
			return ReadShort(buffer, ref offset);
		}

		public static short ReadShort(byte[] buffer, ref int offset)
		{
			var ret = IPAddress.NetworkToHostOrder(BitConverter.ToInt16(buffer, offset));
			offset += 2;
			return ret;
		}

		public static string ReadString(byte[] buffer, int offset, int count)
		{
			return ReadString(buffer, ref offset, count);
		}

		public static string ReadString(byte[] buffer, ref int offset, int count)
		{
			var s = Encoding.ASCII.GetString(buffer, offset, count);
			offset += count;
			return s;
		}

		public static int Write(byte[] buffer, int offset, byte value)
		{
			buffer[offset] = value;
			return 1;
		}

		public static int Write(byte[] dest, int destOffset, byte[] src, int srcOffset, int count)
		{
			Buffer.BlockCopy(src, srcOffset, dest, destOffset, count);
			return count;
		}

		public static int Write(byte[] buffer, int offset, ushort value)
		{
			return Write(buffer, offset, (short)value);
		}

		public static int Write(byte[] buffer, int offset, short value)
		{
			offset += Write(buffer, offset, (byte)(value >> 8));
			offset += Write(buffer, offset, (byte)value);
			return 2;
		}

		public static int Write(byte[] buffer, int offset, int value)
		{
			offset += Write(buffer, offset, (byte)(value >> 24));
			offset += Write(buffer, offset, (byte)(value >> 16));
			offset += Write(buffer, offset, (byte)(value >> 8));
			offset += Write(buffer, offset, (byte)(value));
			return 4;
		}

		public static int Write(byte[] buffer, int offset, uint value)
		{
			return Write(buffer, offset, (int)value);
		}

		public static int Write(byte[] buffer, int offset, long value)
		{
			offset += Write(buffer, offset, (int)(value >> 32));
			offset += Write(buffer, offset, (int)value);
			return 8;
		}

		public static int Write(byte[] buffer, int offset, ulong value)
		{
			return Write(buffer, offset, (long)value);
		}

		public static int Write(byte[] buffer, int offset, byte[] value)
		{
			return Write(buffer, offset, value, 0, value.Length);
		}

		public static int WriteAscii(byte[] buffer, int offset, string text)
		{
			for (var i = 0; i < text.Length; i++) Write(buffer, offset + i, (byte)text[i]);
			return text.Length;
		}

		protected int CheckWritten(int written)
		{
			if (written != ByteLength) throw new Exception("Message encoded incorrectly. Incorrect number of bytes written");
			return written;
		}
	}
}

namespace MonoTorrent.BEncoding
{
	/// <summary>
	/// Class representing a BEncoded string
	/// </summary>
	public class BEncodedString: BEncodedValue, IComparable<BEncodedString>
	{
		byte[] textBytes;
		public string Hex { get { return BitConverter.ToString(TextBytes); } }
		/// <summary>
		/// The value of the BEncodedString
		/// </summary>
		public string Text { get { return Encoding.UTF8.GetString(textBytes); } set { textBytes = Encoding.UTF8.GetBytes(value); } }

		/// <summary>
		/// The underlying byte[] associated with this BEncodedString
		/// </summary>
		public byte[] TextBytes { get { return textBytes; } }

		/// <summary>
		/// Create a new BEncodedString using UTF8 encoding
		/// </summary>
		public BEncodedString(): this(new byte[0]) {}

		/// <summary>
		/// Create a new BEncodedString using UTF8 encoding
		/// </summary>
		/// <param name="value"></param>
		public BEncodedString(char[] value): this(Encoding.UTF8.GetBytes(value)) {}

		/// <summary>
		/// Create a new BEncodedString using UTF8 encoding
		/// </summary>
		/// <param name="value">Initial value for the string</param>
		public BEncodedString(string value): this(Encoding.UTF8.GetBytes(value)) {}


		/// <summary>
		/// Create a new BEncodedString using UTF8 encoding
		/// </summary>
		/// <param name="value"></param>
		public BEncodedString(byte[] value)
		{
			textBytes = value;
		}


		public int CompareTo(object other)
		{
			return CompareTo(other as BEncodedString);
		}

		/// <summary>
		/// Decodes a BEncodedString from the supplied StreamReader
		/// </summary>
		/// <param name="reader">The StreamReader containing the BEncodedString</param>
		internal override void DecodeInternal(RawReader reader)
		{
			if (reader == null) throw new ArgumentNullException("reader");

			int letterCount;
			var length = string.Empty;

			try
			{
				while ((reader.PeekChar() != -1) && (reader.PeekChar() != ':')) // read in how many characters
					length += (char)reader.ReadChar(); // the string is

				if (reader.ReadChar() != ':') // remove the ':'
					throw new BEncodingException("Invalid data found. Aborting");

				if (!int.TryParse(length, out letterCount)) throw new BEncodingException(string.Format("Invalid BEncodedString. Length was '{0}' instead of a number", length));

				textBytes = new byte[letterCount];
				if (reader.Read(textBytes, 0, letterCount) != letterCount) throw new BEncodingException("Couldn't decode string");
			}
			catch (Exception ex)
			{
				if (ex is BEncodingException) throw;
				else throw new BEncodingException("Couldn't decode string", ex);
			}
		}

		/// <summary>
		/// Encodes the BEncodedString to a byte[] using the supplied Encoding
		/// </summary>
		/// <param name="buffer">The buffer to encode the string to</param>
		/// <param name="offset">The offset at which to save the data to</param>
		/// <param name="e">The encoding to use</param>
		/// <returns>The number of bytes encoded</returns>
		public override int Encode(byte[] buffer, int offset)
		{
			var written = offset;
			written += Message.WriteAscii(buffer, written, textBytes.Length.ToString());
			written += Message.WriteAscii(buffer, written, ":");
			written += Message.Write(buffer, written, textBytes);
			return written - offset;
		}


		public override bool Equals(object obj)
		{
			if (obj == null) return false;

			BEncodedString other;
			if (obj is string) other = new BEncodedString((string)obj);
			else if (obj is BEncodedString) other = (BEncodedString)obj;
			else return false;

			return Toolbox.ByteMatch(textBytes, other.textBytes);
		}

		public override int GetHashCode()
		{
			var hash = 0;
			for (var i = 0; i < textBytes.Length; i++) hash += textBytes[i];

			return hash;
		}

		public override int LengthInBytes()
		{
			// The length is equal to the length-prefix + ':' + length of data
			var prefix = 1; // Account for ':'

			// Count the number of characters needed for the length prefix
			for (var i = textBytes.Length; i != 0; i = i/10) prefix += 1;

			if (textBytes.Length == 0) prefix++;

			return prefix + textBytes.Length;
		}

		public override string ToString()
		{
			return Encoding.UTF8.GetString(textBytes);
		}

		public static implicit operator BEncodedString(string value)
		{
			return new BEncodedString(value);
		}

		public static implicit operator BEncodedString(char[] value)
		{
			return new BEncodedString(value);
		}

		public static implicit operator BEncodedString(byte[] value)
		{
			return new BEncodedString(value);
		}

		public int CompareTo(BEncodedString other)
		{
			if (other == null) return 1;

			var difference = 0;
			var length = textBytes.Length > other.textBytes.Length ? other.textBytes.Length : textBytes.Length;

			for (var i = 0; i < length; i++) if ((difference = textBytes[i].CompareTo(other.textBytes[i])) != 0) return difference;

			if (textBytes.Length == other.textBytes.Length) return 0;

			return textBytes.Length > other.textBytes.Length ? 1 : -1;
		}
	}
}