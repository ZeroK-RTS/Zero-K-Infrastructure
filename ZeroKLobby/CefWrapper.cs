using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ZeroKLobby
{
    static class CefWrapper
    {
        [DllImport("cef_wrapper", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, EntryPoint = "initialize")]
        static extern void initialize_([MarshalAs(UnmanagedType.LPStr)] string renderProcessExecutable, int argc, IntPtr argv);
        public static void Initialize(string renderProcessExecutable, string[] argv)
        {
            List<string> args = argv.ToList();
            args.Insert(0, Process.GetCurrentProcess().ProcessName);
            IntPtr ptr = Marshal.AllocHGlobal(IntPtr.Size * args.Count());
            Marshal.Copy(args.Select((s) => Marshal.StringToHGlobalAnsi(s)).ToArray(), 0,
                ptr, args.Count());
            initialize_(renderProcessExecutable, args.Count(), ptr);
            Marshal.FreeHGlobal(ptr);
        }

        [DllImport("cef_wrapper", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, EntryPoint = "deinitialize")]
        static extern void deinitialize_();
        public static void Deinitialize()
        {
            deinitialize_();
            apiFunctions.Clear();
        }

        [DllImport("cef_wrapper", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, EntryPoint = "startMessageLoop")]
        public static extern void StartMessageLoop();

        delegate IntPtr ApiFunc(IntPtr jsonArgs);
        [DllImport("cef_wrapper", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, EntryPoint = "registerApiFunction")]
        static extern void registerApiFunction_([MarshalAs(UnmanagedType.LPStr)] string name, ApiFunc handler);
        static void registerApiFunctionJson(string name, Func<string, string> handler)
        {
            ApiFunc func = (strPtr) =>
            {
                try
                {
                    return Marshal.StringToHGlobalAnsi(handler(Marshal.PtrToStringAnsi(strPtr)));
                }
                catch(ArgumentOutOfRangeException)
                {
                    ExecuteJavascript("throw new Error('" + name + ": Too few arguments');");
                    return Marshal.StringToHGlobalAnsi("null");
                }
                catch (FormatException)
                {
                    ExecuteJavascript("throw new Error('" + name + ": Wrong argument type');");
                    return Marshal.StringToHGlobalAnsi("null");
                }
                catch (Exception e)
                {
                    ExecuteJavascript("throw new Error(\"" + name + ": " + e.Message.Replace("\"", "\\\"").Replace("\n", " ") + "\");");
                    return Marshal.StringToHGlobalAnsi("null");
                }
            };
            apiFunctions.Add(func);
            registerApiFunction_(name, func);
        }

        // There go the long chains of overloads because C# doesn't have variadic generics (this could be done
        // with reflection and casting delegates to object, but I'd rather have type safety.

        // Func<> overloads.
        public static void RegisterApiFunction<R>(string name, Func<R> func)
        {
            registerApiFunctionJson(name, (jsonArgs) =>
            {
                return JsonConvert.SerializeObject(func());
            });
        }
        public static void RegisterApiFunction<R, T1>(string name, Func<T1, R> func)
        {
            registerApiFunctionJson(name, (jsonArgs) =>
            {
                var args = JArray.Parse(jsonArgs);
                return JsonConvert.SerializeObject(func(args[0].ToObject<T1>()));
            });
        }
        public static void RegisterApiFunction<R, T1, T2>(string name, Func<T1, T2, R> func)
        {
            registerApiFunctionJson(name, (jsonArgs) =>
            {
                var args = JArray.Parse(jsonArgs);
                return JsonConvert.SerializeObject(func(args[0].ToObject<T1>(), args[1].ToObject<T2>()));
            });
        }
        public static void RegisterApiFunction<R, T1, T2, T3>(string name, Func<T1, T2, T3, R> func)
        {
            registerApiFunctionJson(name, (jsonArgs) =>
            {
                var args = JArray.Parse(jsonArgs);
                return JsonConvert.SerializeObject(func(args[0].ToObject<T1>(), args[1].ToObject<T2>(), args[2].ToObject<T3>()));
            });
        }
        public static void RegisterApiFunction<R, T1, T2, T3, T4>(string name, Func<T1, T2, T3, T4, R> func)
        {
            registerApiFunctionJson(name, (jsonArgs) =>
            {
                var args = JArray.Parse(jsonArgs);
                return JsonConvert.SerializeObject(func(args[0].ToObject<T1>(), args[1].ToObject<T2>(), args[2].ToObject<T3>(), args[3].ToObject<T4>()));
            });
        }
        public static void RegisterApiFunction<R, T1, T2, T3, T4, T5>(string name, Func<T1, T2, T3, T4, T5, R> func)
        {
            registerApiFunctionJson(name, (jsonArgs) =>
            {
                var args = JArray.Parse(jsonArgs);
                return JsonConvert.SerializeObject(func(args[0].ToObject<T1>(), args[1].ToObject<T2>(), args[2].ToObject<T3>(), args[3].ToObject<T4>(), args[4].ToObject<T5>()));
            });
        }
        public static void RegisterApiFunction<R, T1, T2, T3, T4, T5, T6>(string name, Func<T1, T2, T3, T4, T5, T6, R> func)
        {
            registerApiFunctionJson(name, (jsonArgs) =>
            {
                var args = JArray.Parse(jsonArgs);
                return JsonConvert.SerializeObject(func(args[0].ToObject<T1>(), args[1].ToObject<T2>(), args[2].ToObject<T3>(), args[3].ToObject<T4>(), args[4].ToObject<T5>(), args[5].ToObject<T6>()));
            });
        }

        // Action<> overloads.
        public static void RegisterApiFunction(string name, Action func)
        {
            registerApiFunctionJson(name, (jsonArgs) =>
            {
                func();
                return "null";
            });
        }
        public static void RegisterApiFunction<T1>(string name, Action<T1> func)
        {
            registerApiFunctionJson(name, (jsonArgs) =>
            {
                var args = JArray.Parse(jsonArgs);
                func(args[0].ToObject<T1>());
                return "null";
            });
        }
        public static void RegisterApiFunction<T1, T2>(string name, Action<T1, T2> func)
        {
            registerApiFunctionJson(name, (jsonArgs) =>
            {
                var args = JArray.Parse(jsonArgs);
                func(args[0].ToObject<T1>(), args[1].ToObject<T2>());
                return "null";
            });
        }
        public static void RegisterApiFunction<T1, T2, T3>(string name, Action<T1, T2, T3> func)
        {
            registerApiFunctionJson(name, (jsonArgs) =>
            {
                var args = JArray.Parse(jsonArgs);
                func(args[0].ToObject<T1>(), args[1].ToObject<T2>(), args[2].ToObject<T3>());
                return "null";
            });
        }
        public static void RegisterApiFunction<T1, T2, T3, T4>(string name, Action<T1, T2, T3, T4> func)
        {
            registerApiFunctionJson(name, (jsonArgs) =>
            {
                var args = JArray.Parse(jsonArgs);
                func(args[0].ToObject<T1>(), args[1].ToObject<T2>(), args[2].ToObject<T3>(), args[3].ToObject<T4>());
                return "null";
            });
        }
        public static void RegisterApiFunction<T1, T2, T3, T4, T5>(string name, Action<T1, T2, T3, T4, T5> func)
        {
            registerApiFunctionJson(name, (jsonArgs) =>
            {
                var args = JArray.Parse(jsonArgs);
                func(args[0].ToObject<T1>(), args[1].ToObject<T2>(), args[2].ToObject<T3>(), args[3].ToObject<T4>(), args[4].ToObject<T5>());
                return "null";
            });
        }
        public static void RegisterApiFunction<T1, T2, T3, T4, T5, T6>(string name, Action<T1, T2, T3, T4, T5, T6> func)
        {
            registerApiFunctionJson(name, (jsonArgs) =>
            {
                var args = JArray.Parse(jsonArgs);
                func(args[0].ToObject<T1>(), args[1].ToObject<T2>(), args[2].ToObject<T3>(), args[3].ToObject<T4>(), args[4].ToObject<T5>(), args[5].ToObject<T6>());
                return "null";
            });
        }


        [DllImport("cef_wrapper", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, EntryPoint = "executeJavascript")]
        public static extern void ExecuteJavascript([MarshalAs(UnmanagedType.LPStr)] string code);

        // Store references to registered functions so they don't get GC'd.
        static List<ApiFunc> apiFunctions = new List<ApiFunc>();
    }
}