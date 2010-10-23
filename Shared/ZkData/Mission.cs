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

		public string SanitizedFileName
		{
			get
			{
				var fileName = Name;
				foreach (var character in Path.GetInvalidFileNameChars()) fileName = fileName.Replace(character, '_');
				return fileName + ".sdz"; 	
			}
		}
	}
}
