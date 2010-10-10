#nowarn "9" // allow unverifiable code

// interface to DevIL
// has one public function: unknownToPngBuffer, which converts an image to png

open System
open Tao.DevIl
open Microsoft.FSharp.NativeInterop
open Utils


/// checks the return value of a function and throws an exception if something went wrong
let private getError () =
    let error = Il.ilGetError()
    if error <> Il.IL_NO_ERROR then failwith <| Ilu.iluErrorString error
let private checkError success =  if not success then getError ()

/// initializes DevIL, cleans up when done
/// usage: use il = DevIL.initialize ()
let private initialize () = 
    let imageCount = 1
    let imageNames = Array.zeroCreate imageCount
    Il.ilInit ()
    Il.ilGenImages (imageCount, imageNames)
    Il.ilBindImage (Seq.hd imageNames)
    { new IDisposable with member this.Dispose() = Il.ilDeleteImages (imageCount, imageNames) }  

/// loads an image from a byte lump
let private loadImageBytes format (buffer: byte []) =  Il.ilLoadL (format, buffer, buffer.Length) |> checkError

/// saves the current image in png to a buffer    
let private toBuffer () =
    // get create a buffer with the maximum possible size of the image
    let maxSize = Il.ilGetInteger Il.IL_IMAGE_WIDTH * Il.ilGetInteger Il.IL_IMAGE_HEIGHT * 3 //Il.ilGetInteger Il.IL_IMAGE_BYTES_PER_PIXEL
    let buffer: byte [] = Array.zeroCreate maxSize
    // convert and write the image, keep only the bytes that were actually used
    Array.pin buffer (fun pointer -> Il.ilSaveL(Il.IL_PNG, NativePtr.to_nativeint pointer, maxSize)) |> Array.truncate buffer

/// converts and image to png
let unknownToPngBuffer buffer =
    use il = initialize ()
    loadImageBytes Il.IL_TYPE_UNKNOWN buffer
    toBuffer ()
    
    
