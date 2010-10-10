#light

open System
open System.IO
open PlanetWarsShared
open PlanetWarsShared.Events
open System.Drawing

type Ownership = Core | Arm

let getFactionName = function
    | Core -> "Core"
    | Arm -> "Arm"
    
let getFaction = function 
    | "arm" | "Arm" -> Arm
    | "core" | "Core" -> Core
    | _ -> failwith "unknown faction"
    
let getFactionColor = function
    | Core -> Color.Red
    | Arm -> Color.Blue

type Turn = 
            { Planets : Map<int, Ownership> ; 
              Victories: Map <Ownership, int>; 
              Captures: Map <Ownership, int> ;
              HasMapChanged: bool;
              Date : DateTime;
              Event : Event }

type PWData (galaxy: Galaxy) =

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

        
    let count_by f list = list |> List.filter f |> List.length
        
    let factions = [ Core; Arm ]
    
    let battles = events |> List.choose (function :? BattleEvent as e -> Some e | _ -> None)
          
    let isWinner faction (battle: BattleEvent) = battle.Victor = getFactionName faction

    let battles1v1, battlesNot1v1 = 
        let is1v1 (battle:BattleEvent) = battle.EndGameInfos |> List.of_seq |> count_by (fun e -> not e.Spectator)  <= 2
        battles |> List.partition is1v1
    
    let makeMap l f  = l |> List.map (fun x -> x, f x) |> Map.of_list
    
    let makeFactionMap f = makeMap factions f
        
    let battles1v1ByWinner = 
        makeFactionMap <| fun f -> battles1v1 |> List.filter (isWinner f)
        
    let planetsControlled =
        let aux (armList, coreList) turn =
            let armPlanets, corePlanets = Map.partition (fun k v -> v = Arm) turn.Planets
            ((turn.Date, armPlanets.Count)::armList, (turn.Date, corePlanets.Count)::coreList)
        turns 
        |> List.fold_left aux ([], []) 
        |> fun (armList, coreList) -> [Arm, armList; Core, coreList] 
        |> Map.of_seq

    let accumulateBattles faction =
        List.scan_left (fun acc (b:BattleEvent) -> b.Time, if isWinner faction b then (snd acc) + 1 else snd acc) ((List.hd battles).Time, 0)

    let getVictories faction (battleList: BattleEvent list) = battleList |> accumulateBattles faction
        
    let victories = makeFactionMap (fun f -> getVictories f battles)
        
    let victoriesNo1v1 = makeFactionMap (fun f -> battlesNot1v1 |> getVictories f)
        
    let battleTurns =
        turns |> List.choose (fun t -> match t.Event with :? BattleEvent as e -> Some (t, e) | _ -> None)
    
    let battles1v1Set = new System.Collections.Generic.HashSet<BattleEvent>(battles1v1)
    let turns1v1, turnsNo1v1 = battleTurns |> List.partition (fun (_, b) -> battles1v1Set.Contains b)
            
    let victoriesAndEncirclements = makeFactionMap (fun f -> turnsNo1v1 |> List.map (fun (t, b) -> t.Date, t.Captures.[f] + t.Victories.[f]))
        
    let captures = makeFactionMap (fun f -> turnsNo1v1 |> List.map (fun (t, b) -> t.Date, t.Captures.[f]))
        
    let victories1v1 = makeFactionMap (fun f -> battles1v1ByWinner.[f] |> accumulateBattles f)
    
    let awards =
        galaxy.Players 
        |> Seq.map_concat (fun p -> p.Awards)
        |> Seq.map (fun a -> (a.Text.Split [|','|]).[0])
        |> Seq.count_by id
        |> Seq.sort_by snd
        |> Array.of_seq
        |> Array.unzip
        
    let ranks =
        galaxy.Players
        |> Seq.map (fun p -> p.Rank.ToString ())
        |> Seq.count_by id
        |> Array.of_seq
        |> Array.unzip
        
    let armPlayers, corePlayers =
        galaxy.Players
        |> Array.of_seq
        |> Array.partition (fun p -> p.FactionName = "Arm")    
        
    let credits =
        [| [| armPlayers; corePlayers |] |> Array.map (Array.sum_by (fun p -> float32 p.MetalSpent));
           [| armPlayers; corePlayers |] |> Array.map (Array.sum_by (fun p -> float32 p.MetalAvail)) |]
        
    let playerCount =
        let registeredEvents = events |> List.choose (function :? PlayerRegisteredEvent as e -> Some e | _ -> None)
        registeredEvents |> List.scan_left (fun (t, sum) e -> e.Time.ToOADate (), sum + 1) ((List.hd registeredEvents).Time.ToOADate (), 0) 
        
    let factionPlayers = [| armPlayers.Length; corePlayers.Length |]
    
    let currentPlanetsControlled =
        let planetCounts =  galaxy.Planets |> Seq.count_by (fun p -> p.FactionName) |> Map.of_seq
        [| planetCounts.["Arm"]; planetCounts.["Core"] |]
        
    
    member x.Captures = captures
    member x.VictoriesNo1v1 = victoriesNo1v1
    member x.Victories1v1 = victories1v1
    member x.Victories = victories
    member x.Planets = planetsControlled
    member x.Awards = awards
    member x.Ranks = ranks
    member x.Credits = credits
    member x.PlayerCount = playerCount
    member x.FactionPlayers = factionPlayers
    member x.CurrentPlanetsControlled = currentPlanetsControlled
         
        
