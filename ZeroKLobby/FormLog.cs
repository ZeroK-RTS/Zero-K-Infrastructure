using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ZeroKLobby
{
    public partial class FormLog: Form
    {
        int addedPos;
        static FormLog instance;
        readonly List<TraceLine> lines = new List<TraceLine>();
        readonly object locker = new object();

        readonly Timer repaintTimer = new Timer();
        public static FormLog Instance
        {
            get { return instance ?? (instance = new FormLog()); }
        }

        protected FormLog()
        {
            InitializeComponent();
            repaintTimer = new Timer { Interval = 1000 };
            components = new Container();
            components.Add(repaintTimer);
            repaintTimer.Tick += (s, e) => UpdateLines();
            instance = this;
        }

        public void Notify(bool isError, string text, params object[] args)
        {
            lock (locker)
            {
                if (isError) foreach (var o in args.OfType<Exception>()) ErrorHandling.HandleException(o, false);
				if (args != null && args.Length > 0) text = string.Format(text, args);
				lines.Add(new TraceLine(isError, text));
            }
        }

        void AddLine(DateTime time, string text, Color color)
        {
            tbLog.Select(tbLog.TextLength, 0);
            tbLog.SelectionColor = color;
            tbLog.SelectedText = String.Format("{0}: {1}\n", time, text);
        }

        void UpdateLines()
        {
            lock (locker)
            {
                for (; addedPos < lines.Count; addedPos++)
                {
                    var l = lines[addedPos];
                    AddLine(l.Added, l.Text, l.IsError ? Color.Red : Color.Blue);
                }
            }
        }


        void FormLog_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!Program.CloseOnNext)
            {
                Visible = false;
                e.Cancel = true;
            }
        }

        void FormLog_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape) Close();
        }

        void FormLog_Load(object sender, EventArgs e)
        {
            Icon = ZklResources.ZkIcon;
            UpdateLines();
        }

        void FormLog_VisibleChanged(object sender, EventArgs e)
        {
            if (Visible)
            {
                UpdateLines();
                repaintTimer.Start();
            }
            else repaintTimer.Stop();
        }
    }

    class TraceLine
    {
        public DateTime Added { get; private set; }
        public bool IsError { get; private set; }
        public string Text { get; private set; }

        public TraceLine(bool isError, string text)
        {
            Text = text;
            Added = DateTime.Now;
            IsError = isError;
        }
    }

    public class LogTraceListener: TraceListener
    {
        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message)
        {
            FormLog.Instance.Notify(eventType == TraceEventType.Error || eventType == TraceEventType.Critical, message);
        }

        public override void TraceEvent(TraceEventCache eventCache,
                                        string source,
                                        TraceEventType eventType,
                                        int id,
                                        string format,
                                        params object[] args)
        {
            FormLog.Instance.Notify(eventType == TraceEventType.Error || eventType == TraceEventType.Critical, format, args);
        }

        public override void Write(string message)
        {
            FormLog.Instance.Notify(false, message);
        }

        public override void WriteLine(string message)
        {
            FormLog.Instance.Notify(false, message);
        }
    }
}