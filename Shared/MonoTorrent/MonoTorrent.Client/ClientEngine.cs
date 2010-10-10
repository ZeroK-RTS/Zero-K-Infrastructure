namespace MonoTorrent.Client
{
	/// <summary>
	/// The Engine that contains the TorrentManagers
	/// </summary>
	public class ClientEngine
	{
		internal static readonly BufferManager BufferManager = new BufferManager();
	}
}