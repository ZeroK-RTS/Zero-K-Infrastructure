using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CMissionLib
{
	public class LuaTable
	{
		Dictionary<string, object> Map;

		public LuaTable()
		{
			Map = new Dictionary<string, object>();
		}

		public LuaTable(IEnumerable<object> collection)
		{
			var array = collection.ToArray();
			var map = new Dictionary<string, object>();
			for (var i = 0; i < array.Length; i++)
			{
				map.Add(String.Format("[{0}]", i  + 1), array[i]);
			}
			Map = map;
		}

		public LuaTable(Dictionary<string, object> map)
		{
			Map = map;
		}

		public override string  ToString()
		{
 			 return ToString(0);
		}


		string ToString(int indentLevel)
		{

			if (Map.Count > 0)
			{
				var sb = new StringBuilder();
				foreach (var kvp in Map)
				{
					string value;
					if (kvp.Value is LuaTable) value = ((LuaTable)kvp.Value).ToString(indentLevel + 1);
					else if (kvp.Value is double) value = kvp.Value.ToString();
					else if (kvp.Value is int) value = kvp.Value.ToString();
					else if (kvp.Value is float) value = kvp.Value.ToString();
					else if (kvp.Value is bool) value = kvp.Value.ToString().ToLower();
					else if (kvp.Value is string) value = "[[" + kvp.Value + "]]";
					else throw new Exception("Unable to convert to lua: " + kvp.Value.GetType().Name);

					sb.AppendFormat("{0}{1} = {2},\n", new String('\t', indentLevel + 1), kvp.Key, value);
				}
				return String.Format("{{\n{0}{1}}}", sb, new String('\t', indentLevel));
			}
			return "{}";
		}
	}
}