using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ZkData
{

    public class LuaTable:IEnumerable
    {
        public class Entry
        {
            public string Key;
            public object Value;
            public Entry(string key, object value)
            {
                Key = key;
                Value = value;
            }
        }

        public LuaTable() {}

        List<Entry> data = new List<Entry>();


        public object this[string key]
        {
            get
            {
                var val = data.Find(x => x.Key == key);
                return val;
            }
            set
            {
                var val = data.Find(x => x.Key == key);
                if (val != null) val.Value = value;
                else data.Add(new Entry(key, value));
            }
        }

        public void Add(object value)
        {
            data.Add(new Entry(null, value));
        }

        public void Add(string key, object value)
        {
            this[key] = value;
        }


        public string ToBase64String()
        {
          return Convert.ToBase64String(Encoding.ASCII.GetBytes(ToString()));
        }

      public override string ToString()
        {
            return ToString(0);
        }


			public static string SanitizeString(string input) {
				if (input == null) return null;
				input = input.Replace("\"", " ").Replace("'", " ");
				return Encoding.ASCII.GetString(Encoding.UTF8.GetBytes(input));
			}

    	public string ToString(int indent)
        {
            string shift = "";
            for (int i = 0; i < indent; i++) shift += "  ";
            StringBuilder sb = new StringBuilder();

            
            if (data.Count == 0) return "{}";

            // Don't put {} on separate lines for a single value
            if (data.Count == 1 && data[0].Value != null &&
                !(data[0].Value is LuaTable))
            {
                sb.Append("{ ");
                var kvp = data[0];
                if (kvp.Key != null)
                {
                    sb.Append(kvp.Key);
                    sb.Append(" = ");
                }
                if (kvp.Value is string)
                    sb.AppendFormat("\"{0}\"", SanitizeString(kvp.Value as string));
                else sb.Append(kvp.Value);
                sb.Append(" }");
                return sb.ToString();
            }

            sb.Append("{\n");

            int cnt = 0;
            foreach (var kvp in data) {
                if (kvp.Value != null) {
                    sb.Append(shift);
                    sb.Append("  ");
                    if (kvp.Key != null) {
                        sb.Append(kvp.Key);
                        sb.Append(" = ");
                    }

                    if (kvp.Value is LuaTable)
                        sb.Append((kvp.Value as LuaTable).ToString(indent + 1));
                    else if (kvp.Value is string)
                        sb.AppendFormat("\"{0}\"", SanitizeString(kvp.Value as string));
                    else sb.Append(kvp.Value);
                    cnt++;
                    if (cnt < data.Count) sb.Append(",\n");
                    else sb.Append("\n");
                }
            }


            sb.Append(shift);
            sb.Append("}");
            return sb.ToString();
        }

        public IEnumerator GetEnumerator()
        {
            return data.GetEnumerator();
        }
    }
}
