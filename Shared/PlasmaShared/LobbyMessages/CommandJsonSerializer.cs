using System;
using System.Collections.Generic;
using System.ComponentModel;
using Newtonsoft.Json;
using ZkData;

namespace PlasmaShared.LobbyMessages
{
    public class CommandJsonSerializer
    {
        Dictionary<string, Type> knownTypes = new Dictionary<string, Type>();
        JsonSerializerSettings settings = new JsonSerializerSettings();

        public CommandJsonSerializer()
        {
            RegisterType<Welcome>();
            RegisterType<Login>();
            RegisterType<LoginResponse>();
            RegisterType<Register>();
            RegisterType<RegisterResponse>();

            settings.Formatting =Formatting.None;
            settings.NullValueHandling = NullValueHandling.Ignore;
        }


        public void RegisterTypes(params Type[] types)
        {
            foreach (var t in types) knownTypes[t.Name] = t;
        }

        public void RegisterType<T>()
        {
            var t = typeof(T);
            knownTypes[t.Name] = t;
        }
        
        public object DeserializeLine(string line)
        {
            if (!string.IsNullOrEmpty(line)) {
                var parts = line.Split(new[] { " " }, 2, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 2) throw new Exception(string.Format("Invalid json data {0} : {1}", this, line));
                else {
                    Type type;
                    if (!knownTypes.TryGetValue(parts[0], out type)) {
                        throw  new Exception(string.Format("Invalid json type {0} : {1}", this, parts[0]));
                    } else {
                        return JsonConvert.DeserializeObject(parts[1], type);
                    }
                }
            }
            return null;
        }

        public string SerializeToLine<T>(T value)
        {
            return string.Format("{0} {1}\n",typeof(T).Name, JsonConvert.SerializeObject(value, settings));
        }
    }

    public class Welcome
    {
        public string Version;
        public string Game;
        public string Engine;
    }

    public class Login
    {
        public string Name;
        public string PasswordHash;
    }

    public class Register
    {
        public string Name;
        public string PasswordHash;
    }

    public class RegisterResponse
    {
        public enum Code
        {
            Ok = 0,

            [Description("already connected")]
            AlreadyConnected = 1,

            [Description("name already exists")]
            InvalidName = 2,

            [Description("invalid password")]
            InvalidPassword = 3,

            [Description("banned")]
            Banned = 4
        }

        public Code ResultCode;
        public string Reason;
    }



    public class LoginResponse
    {
        public enum Code
        {
            Ok = 0,
            
            [Description("already connected")]
            AlreadyConnected = 1,
            
            [Description("invalid name")]
            InvalidName = 2,
            
            [Description("invalid password")]
            InvalidPassword = 3,

            [Description("banned")]
            Banned = 4
        }

        public Code ResultCode;
        public string Reason;
    }

}