
open System 
open System.Diagnostics

open FLua.Types
open Tao.Lua
open Utils
open Archive.Types

let private (|.) a b = a + "." + b 

let load fileMap =
    let L = SpringLua.getSpringLuaState fileMap
    let files = Seq.hd (FLua.traceDoString L [] 1 fileMap.["gamedata/defs.lua"].Text)
    Lua.lua_close L
    files

let private toString = function
    | Number n -> string n
    | String s -> s
    | Bool b -> string b
    | v -> failwithf "%O not supported" v
    
let flattenTree defsTree =
    let lines = new ResizeArray<string * string>(60000);
    let rec parseValue path = function
        | k, Table v -> List.iter (parseValue (path |. toString k)) v
        | k, v -> lines.Add (path |. toString k, toString v)
    parseValue "r" (String "defs", defsTree)
    lines |> ResizeArray.map (fun (k, v) -> k.[2..], v) |> Map.of_seq // remove the initial "r." from key strings and make map
    
let getBuildOptions defs unitName =
    let splitLine acc (key, value) = 
        match key with
        | "defs"::"unitdefs"::n::_::[] when n = unitName -> value::acc
        | _ -> acc
    defs |> List.fold splitLine [] |> List.rev
    
let getUnitField defs unitName field = 
    match Map.tryFind ("defs.unitdefs" |. unitName |. field) defs with
    | Some value -> value
    | None -> "n/a"
    
let getTableKeys defs table =
    let splitLine acc (key, _) = 
        match key with
        | "defs"::t::name::_ when t = table -> Set.add name acc
        | _ -> acc
    defs |> List.fold splitLine Set.empty