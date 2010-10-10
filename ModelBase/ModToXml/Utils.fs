open System
open System.Linq
open System.IO
open System.IO.IsolatedStorage
open System.Diagnostics
open System.Text.RegularExpressions
open Printf
open System.Runtime.Serialization.Formatters.Binary
open System.Windows.Media.Imaging

open ICSharpCode.SharpZipLib.Core


module Seq =
    let of_type = Enumerable.OfType
    let mem item source = Seq.exists ((=) item) source
    let groupBy keySelector valueSelector = Seq.groupBy keySelector >> Seq.map (fun (key, keyValuePairs) -> key, Seq.map valueSelector keyValuePairs)
    
    
module Array =
    /// copies the first n elemets of an array to a new one
    let truncate source length = 
        let destination = Array.zeroCreate length
        Array.blit source 0 destination 0 length
        destination

/// writes to debug and console output (Debug.WriteLine, Debug.WriteLine)
let writefn format = ksprintf (fun s -> Debug.WriteLine s; Console.WriteLine s) format

module String =
    let contains key (s: string) = s.Contains key
    let (|Match|_|) pattern input =
        let m = Regex.Match (input, pattern, RegexOptions.IgnoreCase) in
        if m.Success
        then Some (List.tl [ for g in m.Groups -> g.Value ])
        else None

    
/// converts function with two arguments to curried form 
let inline curry2 f x y = f (x, y)
/// converts function to uncurried form with two arguments
let inline uncurry2 f (x, y) = f x y

/// converts function with three arguments to curried form 
let inline curry3 f x y z = f (x, y, z)
/// converts function to uncurried form with three arguments
let inline uncurry3 f (x, y, z) = f x y z

let inline flip f x y = f y x

module Option =
    /// equivalent to match option with Some x -> x | None -> null
    let inline noneToNull option = 
            match option with
            | Some x -> x
            | None -> null
    /// like Option.get, but allows to specify the error message
    let getOrFail message = function
        | Some x -> x
        | None -> failwith message


/// measures the time a function call takes to return, prints result to debug, passes on the return value
let time message f =
    let sw = Stopwatch.StartNew ()
    let ret = f ()
    writefn "%s (%O)" message sw.Elapsed
    ret



/// attemps to delete a file repeatedly until it is successfully deleted or 2 seconds pass        
let safeDelete path =
    let rec delete tries =
        try File.Delete path
        with e ->
            System.Threading.Thread.Sleep 100
            if tries > 20 then rethrow() else delete (tries + 1)
    delete 0

let makeDisposable action = {new IDisposable with member this.Dispose() = action()}
        

type Stream with
    // reads the stream full and places results into a byte array
    member this.ToArray () =
        let buffer = Array.zeroCreate 4096
        let memoryStream = new MemoryStream()
        StreamUtils.Copy(this, memoryStream, buffer)
        memoryStream.ToArray()
        
module Image =
    /// creates an image from a memory lump
    let fromBuffer (buffer: byte array) =
        use stream = new MemoryStream(buffer)
        let image = BitmapImage()
        image.BeginInit()
        image.CacheOption <- BitmapCacheOption.OnLoad
        image.CreateOptions <- BitmapCreateOptions.PreservePixelFormat
        image.StreamSource <- stream
        image.EndInit()
        image.Freeze()
        image
    /// creates an image from a memory lump, resizes the image
    let fromBufferResize (buffer: byte array) width height =
        use stream = new MemoryStream(buffer)
        let image = BitmapImage()
        image.BeginInit()
        image.DecodePixelWidth <- width;
        image.DecodePixelHeight <- height;
        image.CacheOption <- BitmapCacheOption.OnLoad
        image.CreateOptions <- BitmapCreateOptions.PreservePixelFormat
        image.StreamSource <- stream
        image.EndInit()
        image.Freeze()
        image



    
