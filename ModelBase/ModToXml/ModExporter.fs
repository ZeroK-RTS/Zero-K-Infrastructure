namespace ModToXml

open System
open System.Xml
open System.IO

open ICSharpCode.SharpZipLib.Zip
open Tao.Lua

open Utils
open FLua.Types


type ModExporter (springPath: string) =
    
    let getMod = Mod.FromSpringPath springPath
    
    let L = SpringLua.getLuaState springPath
    
    let latestCA = 
        let files = Directory.GetFiles(Path.Combine(springPath, "mods"), "ca-r*.sdz") |> Array.sort
        let isCA = function
            | String.Match "ca-r(\d+)\.sdz$" (revision::[]) as fileName -> Some (revision, fileName)
            | _ -> None
        files|> Array.rev |> Array.tryPick isCA
    
    let writeToXml (fileName: string) revision defsTree =
        let settings = XmlWriterSettings(Indent = true, CheckCharacters = true)
        use textWriter = XmlWriter.Create(fileName, settings)
        let rec writeLuaValue key = function
            | Number n -> textWriter.WriteElementString (key, string n)
            | String s -> textWriter.WriteElementString (key, string s)
            | Bool b -> textWriter.WriteElementString (key, string b)
            | Table values -> 
                let toValidName = FLua.toString >> XmlConvert.EncodeLocalName
                textWriter.WriteStartElement key
                values |> List.iter (fun (key, value) -> writeLuaValue (toValidName key) value) 
                textWriter.WriteEndElement ()
            | v -> failwithf "%O not supported" v
        textWriter.WriteStartDocument()
        textWriter.WriteStartElement "mod"
        textWriter.WriteAttributeString ("modname", "ca")
        revision |> Option.iter (fun r -> textWriter.WriteAttributeString ("revision", r))
        writeLuaValue "defs" defsTree
        textWriter.WriteEndElement ()
        textWriter.WriteEndDocument()
        
    member this.DumpLatestCA xmlFileName = 
        let revision, path = latestCA |> Option.getOrFail "CA not found"
        let modName = SpringLua.getModName L path |> Option.getOrFail "Invalid CA archive"
        writeToXml xmlFileName (Some revision) (getMod modName).LuaDefs
        
    member this.DumpModFromPath(xmlFileName, modPath) =
        let modName = SpringLua.getModName L modPath |> Option.getOrFail "Archive is not mod or not found"
        writeToXml xmlFileName None (getMod modName).LuaDefs
    
    member this.DumpModFromName(xmlFileName, modName) = writeToXml xmlFileName None (getMod modName).LuaDefs
        
        