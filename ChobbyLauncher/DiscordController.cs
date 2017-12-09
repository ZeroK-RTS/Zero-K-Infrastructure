
using System.Diagnostics;

public class DiscordController
{
    public DiscordRpc.RichPresence presence;
    public string discordAppID;
    public string steamAppID;

    public DiscordController(string discordAppId, string steamAppId)
    {
        this.discordAppID = discordAppId;
        this.steamAppID = steamAppId;
    }

    DiscordRpc.EventHandlers handlers;
    
    private bool isInitialized;

    public void Update()
    {
        if (isInitialized) DiscordRpc.RunCallbacks();
    }

    public DiscordRpc.ReadyCallback OnReady { get => handlers.readyCallback; set => handlers.readyCallback = value; }
    public DiscordRpc.DisconnectedCallback OnDisconnected { get => handlers.disconnectedCallback; set => handlers.disconnectedCallback = value; }
    public DiscordRpc.ErrorCallback OnError { get => handlers.errorCallback; set => handlers.errorCallback = value; }
    public DiscordRpc.JoinCallback OnJoin { get => handlers.joinCallback; set => handlers.joinCallback = value; }
    public DiscordRpc.SpectateCallback OnSpectate { get => handlers.spectateCallback; set => handlers.spectateCallback = value; }
    public DiscordRpc.RequestCallback OnRequest { get => handlers.requestCallback; set => handlers.requestCallback = value; }


    public DiscordController()
    {
        handlers = new DiscordRpc.EventHandlers();
    }


    public void Init()
    {
        DiscordRpc.Initialize(discordAppID, ref handlers, true, steamAppID);
        isInitialized = true;
        presence.details = "The best RTS";
        presence.state = "Loading";
        DiscordRpc.UpdatePresence(ref presence);
    }

    public void OnDisable()
    {
        DiscordRpc.Shutdown();
    }

}