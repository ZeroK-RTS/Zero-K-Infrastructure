using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace PlasmaShared
{
 


    public class CommandJsonSerializer
    {
        readonly Dictionary<string, Type> knownTypes = new Dictionary<string, Type>();
        readonly JsonSerializerSettings settings = new JsonSerializerSettings();

        public CommandJsonSerializer(IEnumerable<Type> types)
        {
            RegisterTypes(types.ToArray());
            settings.Formatting = Formatting.None;
            settings.NullValueHandling = NullValueHandling.Ignore;
        }


        public object DeserializeLine(string line)
        {
            if (!string.IsNullOrEmpty(line)) {
                string[] parts = line.Split(new[] { " " }, 2, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 2) throw new Exception(string.Format("Invalid json data {0} : {1}", this, line));
                Type type;
                if (!knownTypes.TryGetValue(parts[0], out type)) throw new Exception(string.Format("Invalid json type {0} : {1}", this, parts[0]));
                return JsonConvert.DeserializeObject(parts[1], type) ?? type.GetConstructor(new Type[] {})?.Invoke(null);
            }
            return null;
        }

        public void RegisterTypes(params Type[] types)
        {
            foreach (Type t in types) knownTypes[t.Name] = t;
        }

        public string SerializeToLine<T>(T value)
        {
            return string.Format("{0} {1}\n", typeof(T).Name, JsonConvert.SerializeObject(value, settings));
        }
    }

   
}