
using System;
using System.Diagnostics;
using ChobbyLauncher;

public class DiscordController : IDisposable
{
    public string discordAppID;
    public string steamAppID;
    DiscordRpc.EventHandlers handlers;
    private bool isInitialized;

    public DiscordController(string discordAppId, string steamAppId)
    {
        this.discordAppID = discordAppId;
        this.steamAppID = steamAppId;
    }

    public DiscordRpc.ReadyCallback OnReady {get { return handlers.readyCallback; } set { handlers.readyCallback = value; }}
    public DiscordRpc.DisconnectedCallback OnDisconnected { get { return handlers.disconnectedCallback; } set { handlers.disconnectedCallback = value; } }
    public DiscordRpc.ErrorCallback OnError { get { return handlers.errorCallback; } set { handlers.errorCallback = value; } }
    public DiscordRpc.JoinCallback OnJoin { get { return handlers.joinCallback; } set { handlers.joinCallback = value; } }
    public DiscordRpc.SpectateCallback OnSpectate { get { return handlers.spectateCallback; } set { handlers.spectateCallback = value; } }
    public DiscordRpc.RequestCallback OnRequest { get { return handlers.requestCallback; } set { handlers.requestCallback = value; } }


    public DiscordController()
    {
        handlers = new DiscordRpc.EventHandlers();
    }


    public void Init()
    {
        try
        {
            DiscordRpc.Initialize(discordAppID, ref handlers, true, steamAppID);
            isInitialized = true;
            var presence = new DiscordUpdatePresence();
            presence.details = "The best RTS";
            presence.state = "Loading";
            DiscordRpc.UpdatePresence(ref presence);
        }
        catch (Exception ex)
        {
            Trace.TraceError("Error initializing discord-RPC: {0}", ex);
        }
    }

    public void Update()
    {
        try
        {
            if (isInitialized) DiscordRpc.RunCallbacks();
        }
        catch (Exception ex)
        {
            Trace.TraceError("Error processing discord-RPC events: {0}", ex);
        }
    }


    public void UpdatePresence(DiscordUpdatePresence presence)
    {
        try
        {
            if (isInitialized) DiscordRpc.UpdatePresence(ref presence);
        }
        catch (Exception ex)
        {
            Trace.TraceError("Error setting discord-RPC presence: {0}", ex);
        }
    }

    public void Respond(string userId, DiscordRpc.Reply reply)
    {
        try
        {
            if (isInitialized) DiscordRpc.Respond(userId, reply);
        }
        catch (Exception ex)
        {
            Trace.TraceError("Error responding to discord-RPC request: {0}", ex);
        }
    }


    public void Dispose()
    {
        try
        {
            DiscordRpc.Shutdown();
        }
        catch (Exception ex)
        {
            Trace.TraceWarning("Exception shutting down discord-RPC: {0}", ex);
        }
    }
}
