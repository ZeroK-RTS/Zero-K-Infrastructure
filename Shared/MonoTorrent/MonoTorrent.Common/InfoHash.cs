using System;
using System.Globalization;
using System.Web;
using MonoTorrent.Common;

namespace MonoTorrent
{
	public class InfoHash: IEquatable<InfoHash>
	{
		readonly byte[] hash;

		internal byte[] Hash { get { return hash; } }

		public InfoHash(byte[] infoHash)
		{
			Check.InfoHash(infoHash);
			if (infoHash.Length != 20) throw new ArgumentException("Infohash must be exactly 20 bytes long");
			hash = (byte[])infoHash.Clone();
		}

		public bool Equals(byte[] other)
		{
			return other == null || other.Length != 20 ? false : Toolbox.ByteMatch(Hash, other);
		}

		public static InfoHash FromHex(string infoHash)
		{
			Check.InfoHash(infoHash);
			if (infoHash.Length != 40) throw new ArgumentException("Infohash must be 40 characters long");

			var hash = new byte[20];
			for (var i = 0; i < hash.Length; i++) hash[i] = byte.Parse(infoHash.Substring(i*2, 2), NumberStyles.HexNumber);

			return new InfoHash(hash);
		}

		public static InfoHash FromMagnetLink(string magnetLink)
		{
			Check.MagnetLink(magnetLink);
			if (!magnetLink.StartsWith("magnet:?")) throw new ArgumentException("Invalid magnet link format");
			magnetLink = magnetLink.Substring("magnet:?".Length);
			var hashStart = magnetLink.IndexOf("xt=urn:btih:");
			if (hashStart == -1) throw new ArgumentException("Magnet link does not contain an infohash");
			hashStart += "xt=urn:btih:".Length;

			var hashEnd = magnetLink.IndexOf('&', hashStart);
			if (hashEnd == -1) hashEnd = magnetLink.Length;
			if (hashEnd - hashStart != 40) throw new ArgumentException("Infohash is not 40 characters long");

			return FromHex(magnetLink.Substring(hashStart, 40));
		}

		public byte[] ToArray()
		{
			return (byte[])hash.Clone();
		}

		public string ToHex()
		{
			return Toolbox.ToHex(Hash);
		}

		public static InfoHash UrlDecode(string infoHash)
		{
			Check.InfoHash(infoHash);
			return new InfoHash(MyHttpUtility.UrlDecodeToBytes(infoHash));
		}

		public string UrlEncode()
		{
			return MyHttpUtility.UrlEncode(Hash);
		}

		public override bool Equals(object obj)
		{
			return Equals(obj as InfoHash);
		}

		public override int GetHashCode()
		{
			// Equality is based generally on checking 20 positions, checking 4 should be enough
			// for the hashcode as infohashes are randomly distributed.
			return Hash[0] | (Hash[1] << 8) | (Hash[2] << 16) | (Hash[3] << 24);
		}

		public override string ToString()
		{
			return BitConverter.ToString(hash);
		}

		public static bool operator ==(InfoHash left, InfoHash right)
		{
			if ((object)left == null) return (object)right == null;
			if ((object)right == null) return false;
			return Toolbox.ByteMatch(left.Hash, right.Hash);
		}

		public static bool operator !=(InfoHash left, InfoHash right)
		{
			return !(left == right);
		}

		public bool Equals(InfoHash other)
		{
			return this == other;
		}
	}
}