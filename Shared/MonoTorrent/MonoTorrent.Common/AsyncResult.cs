using System;
using System.Threading;

namespace MonoTorrent.Common
{
	public class AsyncResult: IAsyncResult
	{
		readonly object asyncState;
		readonly AsyncCallback callback;
		bool completedSyncronously;
		bool isCompleted;
		Exception savedException;
		readonly ManualResetEvent waitHandle;
		internal AsyncCallback Callback { get { return callback; } }

		public AsyncResult(AsyncCallback callback, object asyncState)
		{
			this.asyncState = asyncState;
			this.callback = callback;
			waitHandle = new ManualResetEvent(false);
		}

		public object AsyncState { get { return asyncState; } }

		WaitHandle IAsyncResult.AsyncWaitHandle { get { return waitHandle; } }

		public bool CompletedSynchronously { get { return completedSyncronously; } protected internal set { completedSyncronously = value; } }

		public bool IsCompleted { get { return isCompleted; } protected internal set { isCompleted = value; } }
		protected internal ManualResetEvent AsyncWaitHandle { get { return waitHandle; } }

		protected internal void Complete()
		{
			Complete(savedException);
		}

		protected internal void Complete(Exception ex)
		{
			// Ensure we only complete once - Needed because in encryption there could be
			// both a pending send and pending receive so if there is an error, both will
			// attempt to complete the encryption handshake meaning this is called twice.
			if (isCompleted) return;

			savedException = ex;
			completedSyncronously = false;
			isCompleted = true;
			waitHandle.Set();

			if (callback != null) callback(this);
		}

		protected internal Exception SavedException { get { return savedException; } set { savedException = value; } }
	}
}