using System;

namespace System.Web
{
}

namespace LuaManagerLib
{
	public class Category
	{
		public int id { get; set; }
		public string name { get; set; }
		public int ownerId { get; set; }
		public Category() {}

		public Category(string name, int id)
		{
			this.name = name;
			this.id = id;
		}

		public override String ToString()
		{
			return name;
		}
	}
}