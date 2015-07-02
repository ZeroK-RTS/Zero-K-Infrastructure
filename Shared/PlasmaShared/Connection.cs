#region using

using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

#endregion

namespace ZkData
{
    /// <summary>
    ///     Handles communiction with server on low level
    /// </summary>
    public abstract class Connection
    {
        protected CancellationTokenSource cancellationTokenSource;
        protected bool closeRequestedExplicitly;
        public static Encoding Encoding = new UTF8Encoding(false);
        public virtual bool IsConnected { get; protected set; }
        public string RemoteEndpointIP { get; protected set; }
        public int RemoteEndpointPort { get; protected set; }

        public event EventHandler<string> Input = delegate { };
        public event EventHandler<string> Output = delegate { }; // outgoing command and arguments

        public abstract Task OnConnected();
        public abstract Task OnConnectionClosed(bool wasRequested);
        public abstract Task OnLineReceived(string line);


        /// <summary>
        ///     Closes connection to remote server
        /// </summary>
        public void RequestClose()
        {
            IsConnected = false;
            closeRequestedExplicitly = true;
            if (cancellationTokenSource != null) //in case never connected yet
                cancellationTokenSource.Cancel();
        }


        public abstract Task SendData(byte[] buffer);

        public Task SendString(string line)
        {
            Output(this, line.TrimEnd('\n'));
            return SendData(Encoding.GetBytes(line));
        }

        public override string ToString()
        {
            return string.Format("[{0}:{1}]", RemoteEndpointIP, RemoteEndpointPort);
        }

        protected abstract void InternalClose();

        protected virtual void LogInput(string inp)
        {
            Input(this, inp);
        }

        protected virtual void LogOutput(string outp)
        {
            Output(this, outp);
        }
    }
}