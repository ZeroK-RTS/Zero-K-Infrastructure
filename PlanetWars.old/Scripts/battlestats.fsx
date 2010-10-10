#light

#r "../bin/PlanetWarsShared.dll"
#r "System.Core.dll"

open System
open PlanetWarsShared
open PlanetWarsShared.Events
open PlanetWarsShared.Springie
open System.Linq

let serverString = "tcp://planet-wars.eu:1666/IServer"
let server = (Activator.GetObject (typeof<IServer>, serverString) ) :?> IServer

let galaxy = server.GetGalaxyMap <| AuthInfo("guest", "guest")

let count_by f list = list |> Seq.filter f |> Seq.length

let battles = 
    galaxy.Events
    |> Seq.choose (function :? BattleEvent as e -> Some e | _ -> None)
    |> Seq.filter (fun b -> not b.AreUpgradesDisabled && b.EndGameInfos |> count_by (fun e -> not e.Spectator)  > 2)
    |> List.of_seq
    
let getAverageSkill (team: string) (battle: BattleEvent) =
    battle.EndGameInfos
    |> Seq.filter (fun e -> e.Side.ToLower() = team.ToLower())
    |> Seq.average_by (fun e -> (galaxy.GetPlayer e.Name).RankPoints)

let coreVictories, armVictories = battles |> List.partition (fun b -> b.Victor = "Core")

let coreTerritory, armTerritory = battles |> List.partition (fun b -> (galaxy.GetPlayer (galaxy.GetPlanet b.PlanetID).OwnerName).FactionName = "Core")

printfn "Core Victories: %d %d%%" coreVictories.Length (coreVictories.Length*100/battles.Length)
printfn "Arm Victories %d %d%%" armVictories.Length (armVictories.Length*100/battles.Length)
printfn "Battle fought in core territory: %d %d%%" coreTerritory.Length (coreTerritory.Length*100/battles.Length)
let coreHomeVictories = (coreTerritory |> count_by (fun b -> b.Victor = "Core"))
printfn "Of which Core won: %d %d%%" coreHomeVictories (coreHomeVictories*100/coreTerritory.Length)
printfn "Battle fought in Arm territory: %d %d%%" armTerritory.Length (armTerritory.Length*100/battles.Length)
let armHomeVicotories = (armTerritory |> count_by (fun b -> b.Victor = "Arm"))
printfn "Of which Arm won: %d %d%%"  armHomeVicotories (armHomeVicotories*100/armTerritory.Length)

printfn "---------------------------------------"

coreVictories
|> List.iter (fun b -> printfn "%f,%f" (getAverageSkill "Arm" b) (getAverageSkill "Core" b))

printfn "---------------------------------------"

armVictories
|> List.iter (fun b -> printfn "%f,%f" (getAverageSkill "Arm" b) (getAverageSkill "Core" b))