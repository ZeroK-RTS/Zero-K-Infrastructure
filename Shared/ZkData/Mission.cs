using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ZkData
{
	partial class Mission
	{

		partial void OnCreated()
		{
			ModifiedTime = DateTime.UtcNow;
			CreatedTime = DateTime.UtcNow;
		}

		public static string SanitizeFileName(string fileName)
		{
			foreach (var character in Path.GetInvalidFileNameChars()) fileName = fileName.Replace(character, '_');
			return fileName;
		}
	}
}
