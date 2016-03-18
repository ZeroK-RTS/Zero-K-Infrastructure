using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ZeroKLobby
{
    static class CefWrapper
    {
        // Initialize CEF, taking the path for the render subprocess executable and the program arguments.
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
        [DllImport("cef_wrapper", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, EntryPoint = "initialize")]
        static extern void initialize_([MarshalAs(UnmanagedType.LPStr)] string renderProcessExecutable, int argc, IntPtr argv);

        // Deinitialize CEF.
        public static void Deinitialize()
        {
            deinitialize_();
            apiFunctions.Clear();
            schemaHandler = null;
            if (schemaHandlerData != IntPtr.Zero)
                Marshal.FreeHGlobal(schemaHandlerData);
        }
        [DllImport("cef_wrapper", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, EntryPoint = "deinitialize")]
        static extern void deinitialize_();

        // Open url in a browser window and start CEF message loop. This function will block until the window
        // is closed. bgColor is a string describing the default background color of the browser window in CSS
        // format, like "black" or "rgb(20, 50, 100)". If fullscreen is true the window will be shown in
        // borderless fullscreen mode.
        [DllImport("cef_wrapper", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, EntryPoint = "startMessageLoop")]
        public static extern void StartMessageLoop([MarshalAs(UnmanagedType.LPStr)] string url, [MarshalAs(UnmanagedType.LPStr)] string bgColor, bool fullscreen);

        // Execute arbitrary Javascript code in the main frame. Can be called from any thread.
        [DllImport("cef_wrapper", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, EntryPoint = "executeJavascript")]
        public static extern void ExecuteJavascript([MarshalAs(UnmanagedType.LPStr)] string code);

        // Register the handler to be used with cef:// URLs.
        // The handler should set type to the appropriate mime type for the request. If the returned mime type
        // is empty, the library will try to guess one based on the extension.
        // The handler will be called on a separate IO thread, not the one that called startMessageLoop().
        public delegate byte[] AppSchemaHandler(string url, out string mimeType);
        public static void RegisterAppSchemaHandler(AppSchemaHandler handler)
        {
            AppSchemaHandler_ handler_ = ((url, mimeTypePtr, dataPtr) =>
            {
                string mimeType;
                byte[] data = handler(url, out mimeType);
                if (schemaHandlerData != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(schemaHandlerData);
                    schemaHandlerData = IntPtr.Zero;
                }
                if (data != null)
                {
                    schemaHandlerData = Marshal.AllocHGlobal(data.Length);
                    Marshal.Copy(data, 0, schemaHandlerData, data.Length);
                    Marshal.WriteIntPtr(dataPtr, schemaHandlerData);
                    byte[] mimeTypeBytes = mimeType.Select(c => (byte)c).Take(255).ToArray();
                    Marshal.Copy(mimeTypeBytes, 0, mimeTypePtr, mimeTypeBytes.Length);
                    Marshal.WriteByte(mimeTypePtr, mimeTypeBytes.Length, 0);
                    return data.Length;
                }
                else
                {
                    return -1;
                }
            });
            schemaHandler = handler_;
            registerAppSchemaHandler_(handler_);
        }
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate int AppSchemaHandler_([MarshalAs(UnmanagedType.LPStr)] string url, IntPtr mimeType, IntPtr data);
        [DllImport("cef_wrapper", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, EntryPoint = "registerAppSchemaHandler")]
        static extern void registerAppSchemaHandler_(AppSchemaHandler_ handler_);

        // Register a handler for an API function. The function will be accessible to Javascript in the main frame
        // in the global CefWrapperAPI object. JS code can call the function with a function(result){ ... } callback
        // as the last argument to retrieve the return value. The callback can be omitted.
        // The handler will be called on the thread that called startMessageLoop().

        // There go the long chains of overloads because C# doesn't have variadic generics (this could be done
        // with reflection and casting delegates to object, but I'd rather have type safety.

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

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
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
                catch (ArgumentOutOfRangeException)
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

        // Convert a utf8 string mangled by the marshaller into a proper utf8 string.
        public static string unmangleUtf8(string str)
        {
            return Encoding.UTF8.GetString(Encoding.Default.GetBytes(str));
        }
        // Mangle a string so it gets marshalled into utf8 on the C side.
        public static string mangleUtf8(string str)
        {
            return Encoding.Default.GetString(Encoding.UTF8.GetBytes(str));
        }


        // Store references to registered functions so they don't get GC'd.
        static List<ApiFunc> apiFunctions = new List<ApiFunc>();
        static AppSchemaHandler_ schemaHandler = null;

        // Hold on to data returned from schema handler.
        static IntPtr schemaHandlerData = IntPtr.Zero;
    }
}