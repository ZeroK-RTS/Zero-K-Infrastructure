using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace PlasmaShared
{
    public class CommandJsonSerializer
    {
        private readonly Dictionary<string, Type> knownTypes = new Dictionary<string, Type>();
        //   NetJSONSettings ns = new NetJSONSettings() {DateFormat = NetJSONDateFormat.ISO, UseEnumString = false};
        private JsonSerializerSettings settings = new JsonSerializerSettings() { Formatting = Formatting.None, NullValueHandling = NullValueHandling.Ignore};

        public static string GetTypeNameWithoutGenericArity(Type t)
        {
            string name = t.Name;
            int index = name.IndexOf('`');
            return index == -1 ? name : name.Substring(0, index);
        }        
        
        public CommandJsonSerializer(IEnumerable<Type> types)
        {
            //NetJSON.NetJSON.IncludeFields = false;
            RegisterTypes(types.ToArray());
        }


        /// <summary>
        /// Deserializes a command from a JSON string in a format "CommandName JsonSerializedCommandContent"
        /// </summary>

        public object DeserializeLine(string line)
        {
            if (!string.IsNullOrEmpty(line))
            {
                var parts = line.Split(new[] { " " }, 2, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 2) throw new Exception(string.Format("Invalid json data {0} : {1}", this, line));
                Type type;
                if (!knownTypes.TryGetValue(parts[0], out type)) throw new Exception(string.Format("Invalid json type {0} : {1}", this, parts[0]));
                return JsonConvert.DeserializeObject(parts[1], type, settings) ?? type.GetConstructor(new Type[] { })?.Invoke(null);
            }
            return null;
        }

        public void RegisterTypes(params Type[] types)
        {
            foreach (var t in types) knownTypes[GetTypeNameWithoutGenericArity(t)] = t;
        }

        /// <summary>
        /// Returns name of the class and content of the object in a single line
        /// </summary>
        public string SerializeToLine(object value)
        {
            var send1 = JsonConvert.SerializeObject(value, settings);
            var send = $"{GetTypeNameWithoutGenericArity(value.GetType())} {send1}\n";
            return send;
        }
    }
}