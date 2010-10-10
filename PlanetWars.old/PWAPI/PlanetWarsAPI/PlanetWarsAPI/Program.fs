#light

open System
open System.Net
open System.Net.Sockets
open System.IO
open PlanetWarsShared
open System.Threading
open System.Diagnostics

let log message =
    let msg = sprintf "[%s] %s" (DateTime.Now.ToLongTimeString ())  message
    printfn "%s" msg
    File.AppendAllText ("debug.txt", msg + "\n")

log "getting state"
let serverString = "tcp://licho.eu:1666/IServer"
let getServer () = Activator.GetObject (typeof<IServer>, serverString) :?> IServer
let mutable server = getServer ()
let locker = new obj()
    
let getPlayerRanks = 
    
        let authInfo = AuthInfo("guest", "guest")
        let cachedDate = ref DateTime.MinValue
        let getRanks () =
            cachedDate := DateTime.Now
            let galaxy = server.GetGalaxyMap authInfo
            galaxy.Players |> Seq.map (fun p -> p.Name, p.Rank.value__) |> dict
        let cachedRanks = ref (getRanks ())
        let refreshRanks () =
            let sw = Stopwatch.StartNew ()
            let temp = getRanks ()
            log <| sprintf "ranks loaded in %d ms" sw.ElapsedMilliseconds
            lock (fun locker -> cachedRanks := temp) |> ignore
        fun () ->
            try 
                if server.LastChanged > (!cachedDate).AddMinutes 5. then
                    refreshRanks ()
                !cachedRanks
            with e -> 
                server <- getServer ()
                log <| sprintf "retrying (%s)" e.Message
                refreshRanks ()
                !cachedRanks
    
                
let handleClient (state: obj) = 
    try 
        let client = state :?> TcpClient
        let sw = Stopwatch.StartNew ()
        File.AppendAllText ("IPlog.txt", client.Client.RemoteEndPoint.ToString () + "\n")
        use stream = client.GetStream ()
        use output = new StreamWriter(stream, AutoFlush = true)
        use input = new StreamReader(stream)  
        try
            let query = input.ReadLine ()
            let sw2 = Stopwatch.StartNew ()
            let response =
                let getRank player =
                    let ranks = getPlayerRanks ()
                    match ranks.TryGetValue player with
                    | true, rank -> rank.ToString ()
                    | false, _ -> "-1"
                match String.split [' '] query with 
                | "/getranks"::players -> players |> List.map getRank |> String.concat " "
                | _ ->  "error: unrecognized command"
            log <| sprintf "loaded data in %d ms" sw2.ElapsedMilliseconds
            output.WriteLine response
            output.Flush ()
        with e ->  
            output.WriteLine ("error: " + e.Message)
            output.Flush ()
        log <| sprintf "replied in %d ms" sw.ElapsedMilliseconds 
        client.Close ()
    with e -> log <| "outer error: " + e.Message
    
let socket = TcpListener(IPAddress.Any, 2666)

log "opening socket"
socket.Start()
log "waiting for connection"
while true do
    let client = socket.AcceptTcpClient()
    log <| sprintf "accepted connection from %A" client.Client.RemoteEndPoint
    ThreadPool.QueueUserWorkItem  (WaitCallback handleClient, client) |> ignore



    