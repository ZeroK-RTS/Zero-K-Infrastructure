#light

#r "System.Drawing.dll"
#r "System.Windows.Forms"
#r "PlanetWarsShared.dll"
#r "PlanetWars.exe"
#r "FSharp.PowerPack.dll"

open System
open System.IO
open System.Drawing
open System.Collections.Generic
open System.Windows.Forms
open PlanetWars.UnitSyncLib
open PlanetWarsShared


let springPath = @"D:\Spring"
let toExportPath p = Path.Combine ("C:\\Export", p)
let minimapPath = toExportPath "Minimaps"
let heightmapPath = toExportPath "Heightmaps"
let metalmapPath = toExportPath "Metalmaps"
let mapInfoPath = toExportPath "MapInfo"
let validMapPath = "validmaps.txt"
let mapListPath = "mapList.txt"

module utils = 
    let always a _ = a
    let pair (kvp: KeyValuePair<_, _>) = kvp.Key, kvp.Value
    let pvalue (kvp: KeyValuePair<_, _>) = kvp.Value
    let pkey (kvp: KeyValuePair<_, _>) = kvp.Key
    let noCase = StringComparison.InvariantCultureIgnoreCase

open utils
    
Directory.CreateDirectory minimapPath
Directory.CreateDirectory heightmapPath
Directory.CreateDirectory metalmapPath
Directory.CreateDirectory mapInfoPath

let validMaps = File.ReadAllLines validMapPath

let getMaps =
    use unitSync = new UnitSync(springPath)
    
    let mapNames = unitSync.GetMapNames () |> Seq.map pvalue |> Array.of_seq
         
    let chosenMaps = [ for map in mapNames do
                       let upperMap = map.ToUpper ()
                       for validMap in validMaps do
                       let upperValidMap = validMap.ToUpper ()
                       if upperMap.Contains upperValidMap then yield validMap, map ]
                 
    let missingMaps = [ let upperMaps = mapNames |> Array.map (fun s -> s.ToUpper ()) 
                        for validMap in validMaps do
                        let upperValidMap = validMap.ToUpper ()
                        if not ( upperMaps |> Seq.exists (fun m -> m.Contains upperValidMap) ) then yield validMap ]
                        
    let mapsNotMatched = [ for validMap in validMaps do
                           if not (Seq.exists ((=) validMap) mapNames) 
                           then yield validMap, mapNames |> Seq.filter (fun m -> m.Contains validMap) ]
                           
    Seq.iter (fun (a, b) -> printfn "%s : %A" a b) mapsNotMatched
    
//    printfn "----------------------------------------"
//    printfn "%d/%d" (Seq.length chosenMaps) (Seq.length validMaps)
//    Seq.iter (printfn "%s") missingMaps
//    printfn "----------------------------------------"
//    Seq.iter (fun (a, b) -> printfn "%s : %s" a b) chosenMaps
    
MessageBox.Show "Done!" |> ignore