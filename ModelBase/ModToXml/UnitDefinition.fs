namespace ModToXml

open System
open System.IO

open Utils
open FLua.Types
open Archive.Types

// gets unit names and their buildpic
type UnitDefinition (modVfsMap: Map<_, ArchiveFileData>, unitLua) = 
    let unitName = FLua.getStringField "unitname" unitLua
    let fullName = FLua.getStringField "name" unitLua
    let extensions = [".png"; ".bmp"; ".jpg"]
    let buildpicFileName = 
        match FLua.getLuaField "buildpic" unitLua with
        | None -> 
            extensions
            |> List.map  (fun ext -> ("unitpics/" + unitName + ext).ToLower())
            |> List.tryFind (fun f -> Map.contains f modVfsMap)
        | Some n ->  "unitpics/" + (FLua.toString n).ToLower() |> Some
    let buildpicFromFileInfo (fileData: ArchiveFileData) =
        let bytes = DevIL.unknownToPngBuffer fileData.Bytes 
        bytes |> Image.fromBuffer |> Some
    let buildpic = 
        buildpicFileName 
        |> Option.bind (fun buildpicFile -> Map.tryFind buildpicFile modVfsMap |> Option.bind buildpicFromFileInfo) 
        |> Option.noneToNull

        
    member this.Name = unitName
    member this.FullName = fullName
    member this.BuildPic = buildpic