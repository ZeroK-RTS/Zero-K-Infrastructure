namespace ModToXml

open System
open System.IO
open System.Threading
open System.Text

open Tao.Lua

/// gets the list of maps or mods
type ArchiveLister (springPath: string) = 
    let L = SpringLua.getLuaState springPath
    let contentFolders = ["mods"; "maps"; "base"]
    let files = 
        contentFolders
        |> List.collect (fun path -> Directory.GetFiles(Path.Combine(springPath, path), "*") |> List.of_array)
        |> List.filter (fun path -> path.ToLower().EndsWith ".sdz" || path.ToLower().EndsWith ".sd7")
        |> List.collect (fun archive -> try Archive.listFiles archive |> List.map (fun file -> file, archive) with _ -> [])
    member this.GetMods() =
        files
        |> List.filter (fun (file, _) -> file.ToLower() = "modinfo.tdf" || file.ToLower() = "modinfo.lua")
        |> List.choose (fun (file, archive) -> SpringLua.protectedGetModName L archive |> Option.map (fun name -> name, archive))
        |> List.filter (fun (name, _) -> not (name.StartsWith "Mission:"))
    member this.GetMaps() =
        files
        |> List.filter (fun (file, _) -> file.ToLower().EndsWith ".smf")  
        |> List.map (fun (file, archive) -> Path.GetFileName file, archive)
    interface IDisposable with
        member this.Dispose() = Lua.lua_close L