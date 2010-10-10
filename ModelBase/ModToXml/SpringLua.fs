// Lua utilities for reading spring archives


open System
open System.IO

open Tao.Lua
open FLua.Types
open Archive.Types
open Utils

        
// creates a lua state and registers the TDF parser
let getLuaState springPath =
    if Path.HasExtension springPath then failwith "invalid spring path"
    let L = Lua.luaL_newstate()
    Lua.luaL_openlibs L |> ignore
    let springContent = Path.Combine (springPath, @"base\springcontent.sdz")
    let tdfParserString = Archive.extractTextFile springContent "gamedata/parse_tdf.lua"
    FLua.doStringPushReturn L tdfParserString [] (Constant 1) // push the table containing the parser functions
    Lua.lua_setglobal (L, "TDFparser") // make it a global
    L

// creates a lua state and registers functions commonly used in lua defs    
let getSpringLuaState (fileMap: Map<string, ArchiveFileData>) =
    let L = Lua.luaL_newstate()
    Lua.luaL_openlibs L |> ignore

    // it seems CA makes lowerkeys global, so lets do the same
    do  
        // push the system table
        FLua.doStringPushReturn L (Map.find "gamedata/system.lua" fileMap).Text [] (Constant 1) 
        // get the lowerkeys field from the system table and push it
        Lua.lua_pushstring (L, "lowerkeys") 
        Lua.lua_gettable (L, -2) 
        // set the lowerkeys function as global
        Lua.lua_setglobal (L, "lowerkeys")
        
    let VFS_Include L = // string -> ?
        let path = 
            match FLua.expectArgs L 1 |> Seq.hd with
            | String s -> s.ToLower()
            | args -> failwithf "wrong argument %A" args
        match Map.tryFind path fileMap with
        | Some file -> FLua.doStringPushReturn L fileMap.[path].Text [] (Constant 1)
        | None -> failwithf "path not found: %s" path
        1
        
    let VFS_LoadFile L = // string -> string | nil * string
        let path = 
            match FLua.expectArgs L 1 |> Seq.hd with
            | String s -> s.ToLower()
            | args -> failwithf "wrong argument %A" args
        match Map.tryFind path fileMap with
        | Some file -> FLua.returnValues L [String file.Text]
        | None -> FLua.returnValues L [String "not found"; Nil]
            
    let VFS_FileExists L = // string -> bool
        let path = 
            match FLua.expectArgs L 1 |> Seq.hd with
            | String s -> s.ToLower()
            | args -> failwithf "wrong argument %A" args
        FLua.returnValues L [Bool (Map.contains path fileMap)]
        
    let VFS_DirList L = // string * string -> string array
        let path, mask =
            match FLua.expectArgs L 2 with
            | String path::String mask::[] -> path.ToLower() , mask.ToLower()
            | args -> failwithf "wrong arguments: %A" args
        let files = 
            fileMap 
            |> Map.to_list 
            |> List.map fst 
            |> List.filter (fun s -> s.StartsWith path && s.EndsWith mask.[1..])
            |> List.mapi (fun i v -> Number (i + 1 |> double), String v)
        FLua.returnValues L [Table files]

    let Spring_TimeCheck L = // string * (nil -> nil) -> nil
        let desc = FLua.readString L 1
        Lua.lua_pushvalue (L, 2)
        time desc (fun () -> FLua.traceCallPushReturn L [] (Constant 0)) // call function on top, push return values on stack
        0
        
    let Spring_Echo = FLua.luaPrint ignore //Debug.Write // ? -> nil

    // morphs defs crash if they can't figure out what kind of commander we're using (why do we need morph defs anyway, here?)
    let Spring_GetModOptions L = FLua.returnValues L [Table[String "commtype", String "default" ]] // nil -> table
        
    FLua.setGlobal L "Spring" 
        (Table 
            [String "TimeCheck", Function (Lua.lua_CFunction Spring_TimeCheck)
             String "Echo", Function (Lua.lua_CFunction Spring_Echo)
             String "GetModOptions", Function (Lua.lua_CFunction Spring_GetModOptions)]) 

    FLua.setGlobal L "VFS" 
        (Table 
            [String "Include", Function (Lua.lua_CFunction VFS_Include)
             String "LoadFile", Function (Lua.lua_CFunction VFS_LoadFile)
             String "FileExists", Function (Lua.lua_CFunction VFS_FileExists)
             String "DirList", Function (Lua.lua_CFunction VFS_DirList)])
             
    L
    
let getTdfTableFromString L fileString =
    use p = FLua.delayPop L 1 // pop when we're done
    Lua.lua_getglobal (L, "TDFparser") // push the parser table on the stack
    Lua.lua_getfield (L, -1, "ParseText") // push the parse string function
    FLua.traceCall L [String fileString] 2
    
let getTdfField fieldName L archive  = 
    let extractor = Archive.openArchive archive
    let hasModInfo extension =
        Archive.listFilesFromExtractor extractor |> Seq.tryFind (fun f -> f.ToLower() = "modinfo." + extension)
    match hasModInfo "tdf", hasModInfo "lua" with
    | Some file, _ ->
        let modInfoTable = Archive.extractTextFile archive file |> getTdfTableFromString L
        let modInfo =
            match Seq.hd modInfoTable with
            | Nil -> failwithf "error in file %s: %s:" archive (FLua.toString modInfoTable.[1])
            | value -> FLua.getLuaField "mod" value |> Option.getOrFail "no mod field in modinfo.tdf"
        FLua.getLuaField fieldName modInfo |> Option.map FLua.toString
    | _, Some file ->
        Archive.extractTextFile archive file
        |> FLua.traceDoString L [] 1 
        |> Seq.hd
        |> FLua.getLuaField fieldName
        |> Option.map FLua.toString
    | _ -> None
    
let getModName L path = 
    let makeName (name: string) version = if name.EndsWith version then name else name + " " + version
    getTdfField "name" L path |> Option.bind  (fun name ->  getTdfField "version" L path |> Option.map (fun version -> makeName name version))
    
let protectedGetModName L path =
    try getModName L path
    with _ -> None