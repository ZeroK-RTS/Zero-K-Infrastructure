using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ZeroKLobby
{
    /// <summary>
    ///     Class to provide globally registrable hotkey that work in windows no matter where
    /// </summary>
    public static class GlobalHook
    {
        const int WH_KEYBOARD_LL = 13;
        static readonly IntPtr WM_KEYDOWN = new IntPtr(0x100);
        static readonly IntPtr WM_KEYUP = (IntPtr)0x101;

        static readonly Dictionary<Keys, HookHandler> handlers = new Dictionary<Keys, HookHandler>();
        static readonly IntPtr hhook = IntPtr.Zero;

        /// <summary>
        ///     Delegate to handle global registered keys
        /// </summary>
        /// <param name="key">vkey</param>
        /// <param name="isPressed">is pressed or released - pressed called multiple times</param>
        /// <returns>bool if should be supressed (processed)</returns>
        public delegate bool HookHandler(Keys key, bool isPressed);


        static LowLevelKeyboardProc lowLevelKeyboardProc;

        static GlobalHook()
        {
            IntPtr hInstance = LoadLibrary("User32");
            lowLevelKeyboardProc = hookProc;
            hhook = SetWindowsHookEx(WH_KEYBOARD_LL, lowLevelKeyboardProc, hInstance, 0);
        }

        public static void RegisterHandler(Keys keys, HookHandler handler)
        {
            handlers[keys] = handler;
        }


        public static void UnHook()
        {
            UnhookWindowsHookEx(hhook);
        }

        public static IntPtr hookProc(int code, IntPtr wParam, IntPtr lParam)
        {
            if (code >= 0 && (wParam == WM_KEYUP || wParam == WM_KEYDOWN))
            {
                int vkCode = Marshal.ReadInt32(lParam);

                HookHandler handler;
                if (handlers.TryGetValue((Keys)vkCode, out handler))
                {
                    bool pressed = wParam == WM_KEYDOWN;
                    bool suppress = handler((Keys)vkCode, pressed);
                    if (suppress) return (IntPtr)1;
                }
            }
            return CallNextHookEx(hhook, code, (int)wParam, lParam);
        }

        [DllImport("user32.dll")]
        static extern IntPtr CallNextHookEx(IntPtr idHook, int nCode, int wParam, IntPtr lParam);

        [DllImport("kernel32.dll")]
        static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("user32.dll")]
        static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc callback, IntPtr hInstance, uint threadId);

        [DllImport("user32.dll")]
        static extern bool UnhookWindowsHookEx(IntPtr hInstance);

        delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
    }
}