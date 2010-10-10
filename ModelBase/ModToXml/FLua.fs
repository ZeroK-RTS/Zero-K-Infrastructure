
// F#-Lua interface functions


open System
open Tao.Lua
open Utils

module Types = 

    type LuaValue =
        | Number of double
        | String of string
        | Bool of bool
        | Table of (LuaValue * LuaValue) list
        | Function of Lua.lua_CFunction
        | Nil
        | NoValue
        | Thread
        | Userdata
        | LightUserdata
        
    type LuaType =
        | TypeNumber
        | TypeString
        | TypeBool
        | TypeTable
        | TypeNil
        | TypeNone
        | TypeFunction
        | TypeThread
        | TypeUserdata
        | TypeLightUserdata
    
    /// number of values returned by a lua function
    type ResultCount =
        | Constant of int
        | Variable
        
open Types

/// Store functions delegates so the CLR won't garbage collect them (it can't know they are still in use in the Lua VM).
let keepAlive = 
    let reserve = new ResizeArray<Lua.lua_CFunction>()
    fun f -> reserve.Add f
        
let cbool b = if b then 1 else 0

//let pop f = let ret = f () in Lua.lua_pop (L, 1); ret

/// Pushes value on stack.
/// Supports nested tables, does not support circular tables.
let rec pushValue L value = 
    match value with
    | Number n -> Lua.lua_pushnumber (L, n)
    | String s -> Lua.lua_pushstring (L, s)
    | Bool b -> Lua.lua_pushboolean (L, cbool b)
    | Table pairs -> pushTable L pairs
    | Nil -> Lua.lua_pushnil L
    | Function func -> keepAlive func; Lua.lua_pushcfunction (L, func)
    | _ -> failwith "not implemented"
    
and checkTable L n = 
    match getType L n with
    | TypeTable -> ()
    | t -> failwithf "table expected but found %A" t

/// expects table on top
and setField L key value =
    checkTable L -1
    pushValue L key
    pushValue L value
    Lua.lua_settable (L, -3)
    
/// recursively pushes table
and pushTable L (pairs: (LuaValue * LuaValue) seq) =
    Lua.lua_newtable L
    pairs |> Seq.iter (fun (k, v) -> setField L k v)
    
/// returns type of value at position n    
and getType L n =
    if Lua.lua_gettop L < 1 then failwith "stack empty"
    match Lua.lua_type (L, n) with
        | Lua.LUA_TNUMBER -> TypeNumber
        | Lua.LUA_TSTRING -> TypeString
        | Lua.LUA_TBOOLEAN -> TypeBool
        | Lua.LUA_TFUNCTION -> TypeFunction
        | Lua.LUA_TTABLE -> TypeTable // the tao binding for lua_istable has an error so always use this
        | Lua.LUA_TNIL -> TypeNil
        | Lua.LUA_TNONE -> TypeNone
        | Lua.LUA_TLIGHTUSERDATA -> TypeLightUserdata
        | Lua.LUA_TTHREAD -> TypeThread
        | Lua.LUA_TUSERDATA -> TypeUserdata
        | t -> failwithf "type number not recognized %d" t   

/// Returns value at position n.
/// Supports nested tables, does not support circular tables, leaves stack unchanged.
and readValue L n = 
    match getType L n with
    | TypeNumber -> Number (Lua.lua_tonumber (L, n))
    | TypeString ->  String (Lua.lua_tostring (L, n))
    | TypeBool -> Bool (Lua.lua_toboolean (L, n) <> 0)
    | TypeFunction -> Function (Lua.lua_tocfunction (L, n))
    | TypeTable -> Table (readTable L n)
    | TypeNil -> Nil
    | TypeNone -> NoValue
    | TypeLightUserdata -> LightUserdata
    | TypeThread -> Thread
    | TypeUserdata -> Userdata
    
and pop L n = 
    match Lua.lua_gettop L with
    | s when s < n -> failwithf "stack is too small to pop (s: %d, n: %d)" s n
    | s -> Lua.lua_pop (L, n)
    
and delayPop L n = { new IDisposable with member this.Dispose () = pop L n }

/// expects table at position n
/// recursively reads table at position n, returns all values
and readTable L n =
    checkTable L n
    [ pushValue L Nil
      while Lua.lua_next (L, n - 1) <> 0 do
        let key = readValue L -2
        let value = readValue L -1
        pop L 1
        yield key, value ]

/// sequence representing reversed lua stack
let topStack L = seq { for i = Lua.lua_gettop L downto 1 do yield readValue L i }

/// sequence representing lua stack
let stack L = seq { for i = 1 to Lua.lua_gettop L do yield readValue L i }

/// C#-style null coalescing operator
let (|??) a b = if a = null then b else a 

let checkError L status =
    let getMessage () =
        use p = delayPop L 1
        Lua.lua_tostring (L, -1) |?? "(no error message)"
    match status with
    | 0 -> () // no error
    | Lua.LUA_ERRFILE -> failwithf "%s: File Error" (getMessage ())
    | Lua.LUA_ERRRUN -> failwithf "%s: Runtime Error" (getMessage ())
    | Lua.LUA_ERRSYNTAX -> failwithf "%s: Syntax error" (getMessage ())
    | Lua.LUA_ERRMEM -> failwithf "%s: Memory allocation error" (getMessage ())
    | Lua.LUA_ERRERR -> failwithf "%s: Error function error" (getMessage ())
    | n -> failwithf "Invalid error code %d" n

/// expects table on top
/// returns field from table on top of stack
let getField L key =
    checkTable L -1
    pushValue L key
    use t = delayPop L 1
    Lua.lua_gettable (L, -2)
    readValue L -1

let readString L n =
    match readValue L n with
    | String s -> s
    | v -> failwithf "string expected, found %A" v
    
let checkFunction L n =
    match getType L n with
    | TypeFunction -> ()
    | t -> failwithf "expected function but found %A" t
    
let call L arguments resultNumber =
    let arguments = Array.of_seq arguments
    checkFunction L -1
    Array.iter (pushValue L) arguments
    Lua.lua_pcall (L, Array.length arguments, resultNumber, 0) |> checkError L
    Array.init resultNumber (fun n -> readValue L (-n - 1)) |> Array.rev
    
let callGlobal L functionName =
    Lua.lua_getglobal (L, functionName)
    call
    
let getGlobal L name = 
    use t = delayPop L 1
    Lua.lua_getglobal (L, name)
    readValue L -1

/// loads chunk from string, runs it, pushes results on stack
let doString L s = // tao's dostring causes segfault, use this instead
    Lua.luaL_loadstring(L, s) |> checkError L
    Lua.lua_pcall (L, 0, Lua.LUA_MULTRET, 0) |> checkError L

/// pops n values from stack, returns them    
let expectArgs L n = 
    use t = delayPop L n
    Seq.take n (stack L) |> List.of_seq

/// pushes values on stack, returns number of pushed values
let returnValues L values =
    values |> List.of_seq |> List.rev |> List.iter (pushValue L)
    List.length values 
    
let pushFunction L f = pushValue L (Function (Lua.lua_CFunction f))
    
let registerGlobalFunction L name f =
    pushValue L (Function (Lua.lua_CFunction f))
    Lua.lua_setglobal (L, name)
    
let setGlobal L name value =
    pushValue L value
    Lua.lua_setglobal (L, name)
 
/// calls debug.traceback, pushes result
let traceback L = 
    Lua.lua_getglobal (L, "debug")
    Lua.lua_getfield (L, -1, "traceback")
    Lua.lua_pushvalue (L, 1) // pass error message
    Lua.lua_pushinteger (L, 2) // skip this function and traceback 
    Lua.lua_call (L, 2, 1)  // call debug.traceback
    1     

/// expects function on top
/// runs function on top of stack, pushes return values, shows traceback in case of errors
let traceCallPushReturn L arguments resultCount =
    checkFunction L -1
    List.iter (pushValue L) arguments
    let results = 
        match resultCount with 
        | Constant n -> n
        | Variable -> Lua.LUA_MULTRET
    let baseIndex = Lua.lua_gettop L - arguments.Length  // function index
    pushValue L <| Function (Lua.lua_CFunction traceback) // push traceback function
    Lua.lua_insert (L, baseIndex) // put it under chunk and args
    let status = Lua.lua_pcall (L, arguments.Length, results, baseIndex)
    Lua.lua_remove (L, baseIndex)  // remove traceback function
    if status <> 0 then
        Lua.lua_gc (L, Lua.LUA_GCCOLLECT, 0) |> ignore // force a complete garbage collection in case of errors
        checkError L status
    
/// expects function on top
/// runs function on top of stack, returns values returned from function, shows traceback in case of errors
let traceCall L arguments resultCount =
    traceCallPushReturn L arguments (Constant resultCount)
    use p = delayPop L resultCount
    Array.init resultCount (fun n -> readValue L (-n - 1)) |> Array.rev
    
/// expects function on top
/// runs function on top of stack, no return values, shows traceback in case of errors
let traceCallNoReturn L arguments = traceCallPushReturn L arguments (Constant 0)

/// runs a lua chunk loaded from a string, pushes returned values on stack, shows traceback in case of errors
let doStringPushReturn L s arguments resultCount =  
    Lua.luaL_loadstring (L, s) |> checkError L
    traceCallPushReturn L arguments resultCount
    
/// runs a lua chunk loaded from a string, returns values returned from function, shows traceback in case of errors    
let traceDoString L arguments resultCount s =
    Lua.luaL_loadstring (L, s) |> checkError L
    traceCall L arguments resultCount

/// sends arguments to output function, can be used to make print()-like functions
let luaPrint outputFunc L =
    let n = Lua.lua_gettop L // number of arguments
    Lua.lua_getglobal (L, "tostring")
    for i=1 to n do
        Lua.lua_pushvalue (L, -1) // function to be called
        Lua.lua_pushvalue (L, i) // value to print
        Lua.lua_call (L, 1, 1)
        let s = Lua.lua_tostring (L, 1) // get result
        if s = null then failwith "tostring didn't return string in print()"
        if i > 1 then outputFunc "\t"
        outputFunc s
        pop L 1
    outputFunc "\n"
    0
    
/// returns value that corresponds to a key from a LuaValue.Table map
let getLuaField keyName value = 
    let findStringValue key = function
        | String s, v when s = key -> true
        | _ -> false
    match value with
    | Table keyValuePairs -> keyValuePairs |> List.tryFind (findStringValue keyName) |> Option.map snd
    | _ -> failwith "not table"

/// extracts the the values (and not the keys) from a LuaValue.Table
let getLuaValues = function
    | Table keyValuePairs -> keyValuePairs |> List.map snd
    | _ -> failwith "not table"
    
/// extracts a string from a LuaValue.String or LuaValue.Number
let toString = function
    | String s -> s
    | Number n -> string n
    | _ -> failwith "not string or number"
    
/// gets a string value from a table and key    
let getStringField key = 
    getLuaField key 
    >> Option.getOrFail (sprintf "no string field %s found" key) 
    >> toString
     
