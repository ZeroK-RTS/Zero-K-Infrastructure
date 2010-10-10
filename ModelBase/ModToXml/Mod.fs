namespace ModToXml

open System
open System.IO
open System.Text
open System.Text.RegularExpressions
open System.Windows.Media.Imaging

open Utils
open Tao.Lua
open FLua.Types
open Archive.Types


type Mod (name: string, mods: List<string * string>) =
    // we dont need these archives in the VFS, they will just increase loading time
    let ignoredArchives = 
        [ "bitmaps.sdz"; "cursors.sdz"; "maphelper.sdz"; "otacontent.sdz"; 
          "otacontent.sdz"; "tacontent_v2.sdz"; "tatextures_v062.sdz"; "tacontent.sdz" ]
    let mods = mods |> dict
    let archiveName = mods.[name]
    let modsPath = archiveName + "\\.."
    let springPath = archiveName + "\\..\\.."
    let springContentFile = springPath + "\\base\\springcontent.sdz"
    let L = SpringLua.getLuaState springPath
    let dependencies =
        /// gets all dependencies from an archive recursively
        let rec getDependencies (archive: string) =
            /// translates a mod name or mod archive to a mod archive name, filters unwanted archives
            let toArchivePath name = 
                let name = (FLua.toString name).ToLower()
                let path = if name.EndsWith ".sd7" || name.EndsWith ".sdz" then (mods.[name])  else name
                modsPath + "\\" + path
            // todo: check if this works
            let isUsefulArchive name = ignoredArchives |> List.exists (fun n -> archiveName.EndsWith n)
            let archivePaths = 
                let extractor = Archive.openArchive archive
                /// gets the list of dependencies from modinfo.tdf
                let getDependenciesFromTdf file =
                    let modInfoText = Archive.extractTextFile archive file
                    use p = FLua.delayPop L 1 // pop when we're done
                    Lua.lua_getglobal (L, "TDFparser") // push the parser table on the stack
                    Lua.lua_getfield (L, -1, "ParseText") // push the parse string function
                    let modInfoTable = FLua.traceCall L [String modInfoText] 2 // load the tdf from string
                    let modInfo =
                        match Seq.hd modInfoTable with
                        | Nil -> failwithf "error in file %s: %s:" archive (FLua.toString modInfoTable.[1])
                        | value -> FLua.getLuaField "mod" value |> Option.getOrFail "no mod field in modinfo.tdf" // remove fail?
                    // get all exiting "dependN" fields
                    Seq.unfold (fun n -> FLua.getLuaField (sprintf "depend%d" n) modInfo |> Option.map (fun x -> (x, n + 1))) 0 |> List.of_seq
                /// gets the list of dependencies from modinfo.lua
                let getDependenciesFromLua file =
                    Archive.extractTextFile archive file
                        |> FLua.traceDoString L [] 1 
                        |> Seq.hd
                        |> FLua.getLuaField "depend" 
                        |> Option.getOrFail "no depend table in modinfo.lua" // todo: remove fail?
                        |> FLua.getLuaValues
                let getModInfo extension = Archive.listFilesFromExtractor extractor |> Seq.tryFind (fun f -> f.ToLower() = "modinfo." + extension)
                let archiveNames =
                    match getModInfo "tdf" with
                    | Some file -> getDependenciesFromTdf file
                    | None -> getModInfo "lua" |> Option.getOrFail "not modinfo found" |> getDependenciesFromLua //todo: remove failure?
                archiveNames 
                |> List.filter isUsefulArchive
                |> List.map toArchivePath
                |> List.filter isUsefulArchive
            // return dependencies and the dependencies of the dependencies recursively
            archivePaths @ List.collect getDependencies archivePaths
        getDependencies archiveName |> Seq.distinct |> List.of_seq
    
    // make a map with all the files in the VFS
    // key: filename (lowercase, folders separated with /), value: ArchiveFileData object 
    let loadArchives archives =    
        let readArchive vfsMap archive =
            let extractor = Archive.openArchive archive
            // add the files to the big VFS file map
            Archive.listFilesFromExtractor extractor
            |> Seq.fold (fun state name -> 
                let fileName = name.ToLower().Replace("\\", "/")
                let fileData = LazyFileData(extractor, name) :> ArchiveFileData
                state |> Map.add fileName fileData) vfsMap
        archives |> List.fold readArchive Map.empty
     
    let modVfsMap = loadArchives ([springContentFile] @ dependencies @ [archiveName])
    
    let toModName (path: string) = 
        if path.ToLower().EndsWith ".sdz" || path.ToLower().EndsWith ".sd7" then
            match SpringLua.getModName L path with
            | Some name -> name
            | None -> failwith "invalid mod archive" // todo: remove error
        else path // is already mod name
    let dependencyNames = List.map toModName 
       
    let filterPaths pattern =
        modVfsMap 
        |> Map.to_list
        |> List.choose (fun (path, _) -> if Regex.IsMatch(path, pattern) then Some (Path.GetFileName path) else None)
        |> Seq.of_list
        
    let widgets = filterPaths @"^luaui/widgets/[^/]+?\.lua$"
    let gadgets = filterPaths @"^luarules/gadgets/[^/]+?\.lua$"
    
    let luaDefs =  UnitDefinitionLoader.load modVfsMap
    
    let units =
        luaDefs
        |> FLua.getLuaField "unitdefs"
        |> Option.getOrFail "no unitdefs table in unitdefs"
        |> FLua.getLuaValues
        |> List.map (fun v -> UnitDefinition(modVfsMap, v))
        |> Array.of_list
        
    do Lua.lua_close L
    // load the VFS table functions, which might be used by sidedata
    let L = SpringLua.getSpringLuaState modVfsMap
    
    let sides = 
        modVfsMap
        |> Map.find "gamedata/sidedata.lua"
        |> fun data -> data.Text
        |> FLua.traceDoString L [] 1
        |> Seq.hd
        |> FLua.getLuaValues
        |> List.map (fun v -> FLua.getLuaField "name" v |> Option.getOrFail "side has no name" |> FLua.toString)
        |> List.sort
        
    do Lua.lua_close L
    
    static member FromSpringPath springPath modName =
        use archives = new ArchiveLister(springPath)
        Mod (modName, archives.GetMods())
        
    member this.Widgets = widgets
    member this.Gadgets = gadgets
    member this.Dependencies = dependencies
    member this.Name = name
    member this.UnitDefs = units
    member this.Sides = sides
    member this.LuaDefs = luaDefs
    
    override this.ToString() = this.Name

    