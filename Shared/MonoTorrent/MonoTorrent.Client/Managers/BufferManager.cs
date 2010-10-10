using System;
using System.Collections.Generic;
using MonoTorrent.Common;

namespace MonoTorrent.Client
{
	public enum BufferType
	{
		SmallMessageBuffer,
		MediumMessageBuffer,
		LargeMessageBuffer,
		MassiveBuffer
	}

	public class BufferManager
	{
		public static readonly ArraySegment<byte> EmptyBuffer = new ArraySegment<byte>(new byte[0]);
		internal static readonly int LargeMessageBufferSize = Piece.BlockSize + 32; // 16384 bytes + 32. Enough for a complete piece aswell as the overhead
		internal static readonly int MediumMessageBufferSize = 1 << 11; // 2048 bytes
		internal static readonly int SmallMessageBufferSize = 1 << 8; // 256 bytes

		readonly Queue<ArraySegment<byte>> largeMessageBuffers;
		readonly Queue<ArraySegment<byte>> massiveBuffers;
		readonly Queue<ArraySegment<byte>> mediumMessageBuffers;
		readonly Queue<ArraySegment<byte>> smallMessageBuffers;

		/// <summary>
		/// The class that controls the allocating and deallocating of all byte[] buffers used in the engine.
		/// </summary>
		public BufferManager()
		{
			massiveBuffers = new Queue<ArraySegment<byte>>();
			largeMessageBuffers = new Queue<ArraySegment<byte>>();
			mediumMessageBuffers = new Queue<ArraySegment<byte>>();
			smallMessageBuffers = new Queue<ArraySegment<byte>>();

			// Preallocate some of each buffer to help avoid heap fragmentation due to pinning
			AllocateBuffers(4, BufferType.LargeMessageBuffer);
			AllocateBuffers(4, BufferType.MediumMessageBuffer);
			AllocateBuffers(4, BufferType.SmallMessageBuffer);
		}


		/// <summary>
		/// Returns a buffer to the pool after it has finished being used.
		/// </summary>
		/// <param name="buffer">The buffer to add back into the pool</param>
		/// <returns></returns>
		public void FreeBuffer(ref ArraySegment<byte> buffer)
		{
			if (buffer == EmptyBuffer) return;

			if (buffer.Count == SmallMessageBufferSize) lock (smallMessageBuffers) smallMessageBuffers.Enqueue(buffer);

			else if (buffer.Count == MediumMessageBufferSize) lock (mediumMessageBuffers) mediumMessageBuffers.Enqueue(buffer);

			else if (buffer.Count == LargeMessageBufferSize) lock (largeMessageBuffers) largeMessageBuffers.Enqueue(buffer);

			else if (buffer.Count > LargeMessageBufferSize) lock (massiveBuffers) massiveBuffers.Enqueue(buffer);

				// All buffers should be allocated in this class, so if something else is passed in that isn't the right size
				// We just throw an exception as someone has done something wrong.
			else throw new TorrentException("That buffer wasn't created by this manager");

			buffer = EmptyBuffer; // After recovering the buffer, we send the "EmptyBuffer" back as a placeholder
		}

		/// <summary>
		/// Allocates an existing buffer from the pool
		/// </summary>
		/// <param name="buffer">The byte[]you want the buffer to be assigned to</param>
		/// <param name="type">The type of buffer that is needed</param>
		public void GetBuffer(ref ArraySegment<byte> buffer, int minCapacity)
		{
			if (buffer != EmptyBuffer) throw new TorrentException("The old Buffer should have been recovered before getting a new buffer");

			if (minCapacity <= SmallMessageBufferSize) GetBuffer(ref buffer, BufferType.SmallMessageBuffer);

			else if (minCapacity <= MediumMessageBufferSize) GetBuffer(ref buffer, BufferType.MediumMessageBuffer);

			else if (minCapacity <= LargeMessageBufferSize) GetBuffer(ref buffer, BufferType.LargeMessageBuffer);

			else
			{
				lock (massiveBuffers)
				{
					for (var i = 0; i < massiveBuffers.Count; i++)
					{
						if ((buffer = massiveBuffers.Dequeue()).Count >= minCapacity) return;
						else massiveBuffers.Enqueue(buffer);
					}

					buffer = new ArraySegment<byte>(new byte[minCapacity], 0, minCapacity);
				}
			}
		}


		void AllocateBuffers(int number, BufferType type)
		{
			Logger.Log(null, "BufferManager - Allocating {0} buffers of type {1}", number, type);
			if (type == BufferType.LargeMessageBuffer) while (number-- > 0) largeMessageBuffers.Enqueue(new ArraySegment<byte>(new byte[LargeMessageBufferSize], 0, LargeMessageBufferSize));

			else if (type == BufferType.MediumMessageBuffer) while (number-- > 0) mediumMessageBuffers.Enqueue(new ArraySegment<byte>(new byte[MediumMessageBufferSize], 0, MediumMessageBufferSize));

			else if (type == BufferType.SmallMessageBuffer) while (number-- > 0) smallMessageBuffers.Enqueue(new ArraySegment<byte>(new byte[SmallMessageBufferSize], 0, SmallMessageBufferSize));

			else throw new ArgumentException("Unsupported BufferType detected");
		}

		/// <summary>
		/// Allocates an existing buffer from the pool
		/// </summary>
		/// <param name="buffer">The byte[]you want the buffer to be assigned to</param>
		/// <param name="type">The type of buffer that is needed</param>
		void GetBuffer(ref ArraySegment<byte> buffer, BufferType type)
		{
			// We check to see if the buffer already there is the empty buffer. If it isn't, then we have
			// a buffer leak somewhere and the buffers aren't being freed properly.
			if (buffer != EmptyBuffer) throw new TorrentException("The old Buffer should have been recovered before getting a new buffer");

			// If we're getting a small buffer and there are none in the pool, just return a new one.
			// Otherwise return one from the pool.
			if (type == BufferType.SmallMessageBuffer)
			{
				lock (smallMessageBuffers)
				{
					if (smallMessageBuffers.Count == 0) AllocateBuffers(5, BufferType.SmallMessageBuffer);
					buffer = smallMessageBuffers.Dequeue();
				}
			}

			else if (type == BufferType.MediumMessageBuffer)
			{
				lock (mediumMessageBuffers)
				{
					if (mediumMessageBuffers.Count == 0) AllocateBuffers(5, BufferType.MediumMessageBuffer);
					buffer = mediumMessageBuffers.Dequeue();
				}
			}
           
				// If we're getting a large buffer and there are none in the pool, just return a new one.
				// Otherwise return one from the pool.
			else if (type == BufferType.LargeMessageBuffer)
			{
				lock (largeMessageBuffers)
				{
					if (largeMessageBuffers.Count == 0) AllocateBuffers(5, BufferType.LargeMessageBuffer);
					buffer = largeMessageBuffers.Dequeue();
				}
			}

			else throw new TorrentException("You cannot directly request a massive buffer");
		}
	}
}