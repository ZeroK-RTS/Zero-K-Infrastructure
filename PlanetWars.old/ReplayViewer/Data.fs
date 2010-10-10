#light

open System
open System.IO
open PlanetWarsShared
open PlanetWarsShared.Events

type Ownership = Core | Arm

let getFactionName = function
    | Core -> "Core"
    | Arm -> "Arm"
    
let getFaction = function 
    | "arm" | "Arm" -> Arm
    | "core" | "Core" -> Core
    | _ -> failwith "unknown faction"
    
    
type Turn = { Planets : Map<int, Ownership> ; 
              Victories: Map <Ownership, int>; 
              Captures: Map <Ownership, int> ;
              HasMapChanged: bool;
              Date : DateTime;
              Event : Event }


let galaxy = 
    let serverString = "tcp://localhost:1666/IServer"
    let server = (Activator.GetObject (typeof<IServer>, serverString) ) :?> IServer
    server.GetGalaxyMap <| AuthInfo("guest", "guest")

let flip f x y = f y x

let planets = galaxy.Planets |> Seq.map (fun p -> p.ID, p) |> dict

let links = System.Collections.Generic.Dictionary<int, Link list>()
do for link in galaxy.Links do
    for id in link.PlanetIDs do
        match links.TryGetValue id with
        | true, list -> links.[id] <- link::list
        | false, _ -> links.[id] <- [link]
        
let players = galaxy.Players |> Seq.map (fun p -> p.Name, p) |> dict
let events = List.of_seq galaxy.Events
    

#if false
let galaxy =
    let stateFile = @"C:\Documents and Settings\Administrator\Desktop\serverstate.xml"
    let state = PlanetWarsServer.ServerState.FromFile 
    state.Galaxy
#endif



let turns = 
    let aux (frame: Turn) (event: Event) = 
        match event with
        | :? BattleEvent as e ->
            let victor = (getFaction e.Victor)
            let status = Map.add e.PlanetID (getFaction e.Victor) frame.Planets
            let isEnemy id = 
                match Map.tryfind id status with
                | Some faction when victor = faction -> true 
                | _ -> false
            let encircledPlanets = 
                [ for planetID, ownership in Map.to_list status do
                  let planetLinks = links.[planetID]
                  if List.length planetLinks > 1 && victor <> ownership 
                  then
                     let linkedPlanets = 
                        planetLinks 
                        |> List.map_concat (fun l -> [l.PlanetIDs.[0]; l.PlanetIDs.[1]] ) 
                        |> List.filter ((<>) planetID)
                     if List.for_all isEnemy linkedPlanets then yield planetID ]
            { Planets = List.fold_left (fun state id -> Map.add id victor state) status encircledPlanets;
              Victories = Map.add victor (frame.Victories.[victor] + 1) frame.Victories;
              Captures = Map.add victor (frame.Captures.[victor] + List.length encircledPlanets) frame.Captures;
              HasMapChanged = true; 
              Date = e.Time
              Event = event}
        | :? PlayerRegisteredEvent as e when e.PlanetID.HasValue ->
            let faction = getFaction players.[e.PlayerName].FactionName
            { frame with  
                Planets = Map.add e.PlanetID.Value faction frame.Planets;
                HasMapChanged = true; 
                Date = e.Time;
                Event = event }
        | e -> { frame with 
                    HasMapChanged = false; 
                    Date = e.Time}
    let initialCounts = Map.of_list [Core, 0 ; Arm, 0]
    events
    |> List.sort_by (fun e -> e.Time) 
    |> List.scan_left aux 
        { Planets = Map.empty; 
          Victories = initialCounts; 
          Captures =  initialCounts; 
          HasMapChanged = true; 
          Date = (List.hd events).Time 
          Event = (List.hd events)} 
    
let count_by f list = list |> Seq.filter f |> Seq.length

let is1v1 (battle: BattleEvent) = battle.EndGameInfos |> count_by (fun e -> not e.Spectator)  <= 2
let isWinner faction (battle: BattleEvent) = battle.Victor = getFactionName faction

let battles =
    galaxy.Events
    |> Seq.choose (function :? BattleEvent as e -> Some e | _ -> None)
    |> List.of_seq

let duels = battles |> Seq.filter is1v1 |> List.of_seq
    
let duelsByVictor winner = duels |> List.filter (isWinner winner)
    
let duelsByDayAndVictor victor =
    let byDay (battle: BattleEvent) =
        let origin = DateTime(battle.Time.Year, 1, 1, 0, 0, 0)
        let time = origin.AddDays (float battle.Time.DayOfYear)
        time
    duelsByVictor victor
    |> List.map byDay
    |> Seq.count_by id
    
let duelsByDay =
    let byDay (battle: BattleEvent) =
        let origin = DateTime(battle.Time.Year, 1, 1, 0, 0, 0)
        let time = origin.AddDays (float battle.Time.DayOfYear)
        time
    duels
    |> List.map byDay
    |> Seq.count_by id
    
let planetsByOwner planets = 
    let counts = planets |> Map.to_seq |>  Seq.count_by snd |>  Map.of_seq
    let ensure faction counts =
        match Map.tryfind faction counts with
        | Some _ -> counts
        | None -> Map.add  faction 0 counts
    counts |> ensure Arm |> ensure Core 
    
let factionPlanetsByDate faction =  turns |> List.map (fun t -> t.Planets |> planetsByOwner |> Map.find faction , t.Date)

let accumulateBattles faction =
    List.scan_left (fun acc (b:BattleEvent) -> b.Time, if isWinner faction b then (snd acc) + 1 else snd acc) ((List.hd battles).Time, 0)

let getVictories faction (battleList: BattleEvent list) = battleList |> accumulateBattles faction
    
let victories faction = getVictories faction battles
    
let victoriesNo1v1 faction =
    battles 
    |> List.filter (is1v1 >> not)
    |> getVictories faction
    
let battleTurns =
    turns 
    |> List.of_seq
    |> List.choose (fun t -> match t.Event with :? BattleEvent as e -> Some (t, e) | _ -> None)
        
let turns1v1, turnsNo1v1 = battleTurns |> List.partition (snd >> is1v1)
        
let victoriesAndEncirclements faction =
    turnsNo1v1 |> List.map (fun (t, b) -> t.Date, t.Captures.[faction] + t.Victories.[faction])
    
let captures faction =
    turnsNo1v1 |> List.map (fun (t, b) -> t.Date, t.Captures.[faction])    
    
let victories1v1 faction =
    duelsByVictor faction |> accumulateBattles faction
         
        
