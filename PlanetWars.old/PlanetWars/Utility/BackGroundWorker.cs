// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.ComponentModel;
using System.Threading;

namespace PlanetWars.Utility
{
    public class BackgroundWorker<TIn, TOut> // taken from mono and modified to be generic
    {
        AsyncOperation async;
        bool cancel_pending, support_cancel;

        public bool CancellationPending
        {
            get { return cancel_pending; }
        }

        public bool IsBusy
        {
            get { return async != null; }
        }

        [DefaultValue(false)]
        public bool WorkerReportsProgress { get; set; }

        [DefaultValue(false)]
        public bool WorkerSupportsCancellation
        {
            get { return support_cancel; }
            set { support_cancel = value; }
        }

        public event DoWorkEventHandler<TIn, TOut> DoWork;
        public event ProgressChangedEventHandler ProgressChanged;
        public event RunWorkerCompletedEventHandler<TOut> RunWorkerCompleted;

        public void CancelAsync()
        {
            if (!support_cancel) {
                throw new InvalidOperationException("This background worker does not support cancellation.");
            }

            if (IsBusy) {
                cancel_pending = true;
            }
        }

        public void ReportProgress(int percentProgress)
        {
            ReportProgress(percentProgress, null);
        }

        public void ReportProgress(int percentProgress, object userState)
        {
            if (!WorkerReportsProgress) {
                throw new InvalidOperationException("This background worker does not report progress.");
            }

            // FIXME: verify the expected behavior
            if (!IsBusy) {
                return;
            }

            async.Post(
                delegate(object o)
                {
                    ProgressChangedEventArgs e = o as ProgressChangedEventArgs;
                    OnProgressChanged(e);
                },
                new ProgressChangedEventArgs(percentProgress, userState));
        }

        public void RunWorkerAsync()
        {
            RunWorkerAsync(default(TIn));
        }

        void ProcessWorker(TIn argument, AsyncOperation async, SendOrPostCallback callback)
        {
            // do worker
            Exception error = null;
            DoWorkEventArgs<TIn, TOut> e = new DoWorkEventArgs<TIn, TOut>(argument);
            try {
                OnDoWork(e);
            } catch (Exception ex) {
                error = ex;
            }
            callback(new object[] {new RunWorkerCompletedEventArgs<TOut>(e.Result, error, e.Cancel), async});
        }

        void CompleteWorker(object state)
        {
            object[] args = (object[])state;
            RunWorkerCompletedEventArgs<TOut> e = args[0] as RunWorkerCompletedEventArgs<TOut>;
            AsyncOperation async = args[1] as AsyncOperation;

            SendOrPostCallback callback = delegate(object darg)
            {
                this.async = null;
                OnRunWorkerCompleted(darg as RunWorkerCompletedEventArgs<TOut>);
            };

            async.PostOperationCompleted(callback, e);

            cancel_pending = false;
        }

        public void RunWorkerAsync(TIn argument)
        {
            if (IsBusy) {
                throw new InvalidOperationException("The background worker is busy.");
            }

            async = AsyncOperationManager.CreateOperation(this);

            ProcessWorkerEventHandler handler = ProcessWorker;
            handler.BeginInvoke(argument, async, CompleteWorker, null, null);
        }

        protected virtual void OnDoWork(DoWorkEventArgs<TIn, TOut> e)
        {
            if (DoWork != null) {
                DoWork(this, e);
            }
        }

        protected virtual void OnProgressChanged(ProgressChangedEventArgs e)
        {
            if (ProgressChanged != null) {
                ProgressChanged(this, e);
            }
        }

        protected virtual void OnRunWorkerCompleted(RunWorkerCompletedEventArgs<TOut> e)
        {
            if (RunWorkerCompleted != null) {
                RunWorkerCompleted(this, e);
            }
        }

        delegate void ProcessWorkerEventHandler(TIn argument, AsyncOperation async, SendOrPostCallback callback);
    }
}