using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace MonoTorrent.Client
{
	public static class Logger
	{
		static readonly List<TraceListener> listeners;
		static StringBuilder sb = new StringBuilder();

		static Logger()
		{
			listeners = new List<TraceListener>();
		}

		public static void AddListener(TraceListener listener)
		{
			if (listener == null) throw new ArgumentNullException("listener");

			lock (listeners) listeners.Add(listener);
		}

		public static void Flush()
		{
			lock (listeners) listeners.ForEach(delegate(TraceListener l) { l.Flush(); });
		}

		/*
        internal static void Log(PeerIdInternal id, string message)
        {
            Log(id.PublicId, message);
        }

        internal static void Log(PeerId id, string message)
        {
            lock (listeners)
                for (int i = 0; i < listeners.Count; i++)
                    listeners[i].WriteLine(id.GetHashCode().ToString() + ": " + message);
        }

        internal static void Log(string p)
        {
            lock (listeners)
                for (int i = 0; i < listeners.Count; i++)
                    listeners[i].WriteLine(p);
        }*/

		[Conditional("DO_NOT_ENABLE")]
		internal static void Log(object connection, string message) {}

		[Conditional("DO_NOT_ENABLE")]
		internal static void Log(object connection, string message, params object[] formatting) {}
	}
}