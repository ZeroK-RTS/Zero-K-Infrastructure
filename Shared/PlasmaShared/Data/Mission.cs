using System;
using System.IO;
using System.Runtime.Serialization;

namespace ZkData
{
	partial class Mission
	{
		[Obsolete("Do not use just for serializaiton hack")]
		string authorName;

		[Obsolete("Do not ever write to this!")]
		[DataMember]
		public string AuthorName
		{
			get
			{
				if (authorName != null) return authorName;
				else return Account != null ? Account.Name : null;
			}
			set { authorName = value; }
		}

		public string SanitizedFileName
		{
			get
			{
				var fileName = Name;
				foreach (var character in Path.GetInvalidFileNameChars()) fileName = fileName.Replace(character, '_');
				return fileName + ".sdz";
			}
		}

		partial void OnCreated()
		{
			ModifiedTime = DateTime.UtcNow;
			CreatedTime = DateTime.UtcNow;
		}
	}
}