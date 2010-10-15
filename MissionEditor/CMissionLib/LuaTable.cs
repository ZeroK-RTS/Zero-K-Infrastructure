using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace CMissionLib
{
	public class LuaTable
	{
		Dictionary<object, object> Map;

		public LuaTable()
		{
			Map = new Dictionary<object, object>();
		}

		public static LuaTable Empty
		{
			get
			{
				return new LuaTable(new Dictionary<object, object>());
			}
		}

		public static LuaTable CreateArray<T>(IEnumerable<T> collection)
		{
			var array = collection.ToArray();
			var map = new Dictionary<object, object>();
			for (var i = 0; i < array.Length; i++)
			{
				map.Add(i  + 1, array[i]);
			}
			return new LuaTable(map);
		}

		public static LuaTable CreateSet(IEnumerable<string> collection)
		{
			var map = collection.Distinct().ToDictionary<string, object, object>(item => item, item => true);
			return new LuaTable(map);
		}

		public LuaTable(Dictionary<object, object> map)
		{
			Map = map;
		}


		string ToString(int indentLevel = 0)
		{
			if (Map.Count == 0) return "{}";
			var sb = new StringBuilder();
			foreach (var kvp in Map)
			{

				string key;
				if (kvp.Key is string) key = String.Format("[\"{0}\"]", kvp.Key);
				else if (kvp.Key is int) key = String.Format("[{0}]", kvp.Key);
				else throw new Exception("Unable to convert key type to lua: " + kvp.Key.GetType().Name);

				string value;
				if (kvp.Value is LuaTable) value = ((LuaTable)kvp.Value).ToString(indentLevel + 1);
				else if (kvp.Value is int) value = kvp.Value.ToString();
				else if (kvp.Value is float) value = ((float) kvp.Value).ToString(CultureInfo.InvariantCulture);
				else if (kvp.Value is double) value = ((double) kvp.Value).ToString(CultureInfo.InvariantCulture);
				else if (kvp.Value is bool) value = kvp.Value.ToString().ToLower();
				else if (kvp.Value is string) value = "[[" + kvp.Value + "]]";
				else throw new Exception("Unable to convert value type to lua: " + kvp.Value.GetType().Name);



				sb.AppendFormat("{0}{1} = {2},\n", new String('\t', indentLevel + 1), key, value);
			}
			return String.Format("{{\n{0}{1}}}", sb, new String('\t', indentLevel));
		}
	}
}