#nowarn "40" // don't warn for recursive objects

// module for archive manipulation

open System
open System.Threading
open System.IO
open System.Text

open ICSharpCode.SharpZipLib.Zip
open ICSharpCode.SharpZipLib.Core
open ICSharpCode.SharpZipLib.Checksums
open SevenZip

open Utils

/// 7zip manipulation module for internal use
module SevenZip = 

    /// opens a 7zip archive for reading  
    let openArchive (archive: string) = new SevenZipExtractor(File.OpenRead archive, InArchiveFormat.SevenZip)
    
    // Turn the async method into a blockin one because it's more convenient
    let extractFileAsync (extractor: SevenZipExtractor) (file: string) = 
        Async.Primitive (fun (continuation, errorContinuation, cancelContiuation) ->
            use stream = new MemoryStream()
            let rec handler = EventHandler (fun _ _ -> 
                extractor.FileExtractionFinished.RemoveHandler handler
                stream.Position <- int64 0
                try continuation stream
                with exn -> errorContinuation exn)
            extractor.FileExtractionFinished.AddHandler handler
            extractor.ExtractFile(file.Replace("/", "\\"), stream, true))
            
    /// extract a 7zip file with the specified extractor, return bytes        
    let extractFileWithExtractor extractor file =  (async { return! extractFileAsync extractor file } |> Async.RunSynchronously).ToArray()
    
    /// get the list of files in a 7zip archive
    let listFiles (extractor: SevenZipExtractor) = extractor.ArchiveFileNames |> List.of_seq
    
#if false // crashes when trying to compress pngs
    let createArchive streams (archivePath: string) =
        let compressor = new SevenZipCompressor(true, ArchiveFormat = OutArchiveFormat.Zip)
        let dictionary = new System.Collections.Generic.Dictionary<Stream, string>()
        Seq.iter dictionary.Add streams
        compressor.CompressStreamDictionary(dictionary, archivePath)
#endif


/// Zip manipulation module for internal use
module Zip =

    /// opens a zip archive for reading and writing
    let openArchive (archive: string) = new ZipFile(archive)
    
    /// extract a zip file with the specified extractor, return bytes        
    let extractFileWithExtractor (extractor: ZipFile) (file: string) =
        use stream = extractor.GetInputStream(ZipEntry(file.Replace("\\", "/")))
        stream.ToArray()
        
#if false 
    // spring's VFS can't get the file list from this, it seems
    let createArchive (streams: seq<_ * string>) archivePath =
        let crc32 = Crc32()
        use outputStream = new ZipOutputStream(File.Create archivePath, UseZip64 = UseZip64.Off)
        let buffer = Array.zero_create 4096
        for stream: Stream, entryName in streams do
            let entryBytes = stream.ToArray()
            crc32.Update entryBytes
            let entry = new ZipEntry(entryName, DateTime = DateTime.Now, Crc = crc32.Value)
            crc32.Reset()
            outputStream.PutNextEntry entry
            outputStream.Write(entryBytes, 0, entryBytes.Length)
            outputStream.CloseEntry()
        outputStream.Finish()
        outputStream.Close()
#endif

    /// create an archive, compress a sequence of stream/entry name pairs
    // writes streams to temp files and zips those, as a workaround for the problems of the disabled functions above
    let createArchive streams (archivePath: string) =
        use zipFile = ZipFile.Create archivePath
        let buffer = Array.zeroCreate 4096
        for sourceStream: Stream, entryName: string in streams do
            let tempFilePath = Path.GetTempFileName()
            try
                use fileStream = File.Open(tempFilePath, FileMode.OpenOrCreate, FileAccess.Write)
                StreamUtils.Copy(sourceStream, fileStream, buffer)
            finally
                sourceStream.Dispose()
            zipFile.BeginUpdate()
            zipFile.Add(tempFilePath, entryName)
            zipFile.CommitUpdate()
            Utils.safeDelete tempFilePath
        zipFile.Close()
    
    /// create an archive, add a sequence of stream/entry name pairs    
    let listFiles (archive: ZipFile) = archive |> Seq.cast<ZipEntry> |> Seq.map (fun e -> e.Name) |> List.of_seq




/// represents an archive open for reading        
type Extractor =
    | ZipExtractor of ZipFile
    | SevenZipExtractor of SevenZipExtractor
    interface IDisposable with
        override this.Dispose() =
            match this with
            | ZipExtractor x -> x.Close()
            | SevenZipExtractor x -> x.Dispose()
            
/// represents an archive open for writing  
type Compressor =
    | ZipCompressor of ZipFile
    | SevenZipCompressor of SevenZipCompressor
    interface IDisposable with
        override this.Dispose() =
            match this with
            | ZipCompressor c-> c.Close()
            | SevenZipCompressor c -> ()
            
            
            
            
let private textEncoding = Encoding.GetEncoding "iso-8859-1" //ASCIIEncoding()

/// opens an archive for reading   
let openArchive (archive: string) =
    if archive.ToLower().EndsWith "sdz" 
    then Zip.openArchive archive |> ZipExtractor 
    else SevenZip.openArchive archive |> SevenZipExtractor
    
/// extract a file with the specified extractor, return bytes
let extractFileWithExtractor file = function
    | ZipExtractor x -> Zip.extractFileWithExtractor x file
    | SevenZipExtractor x -> SevenZip.extractFileWithExtractor x file
    
/// extract a file from the specified archive, return bytes
let extractFile archive file = 
    use extractor = openArchive archive
    extractFileWithExtractor file extractor
    
/// extract a text file from the specified archive
let extractTextFile archiveName file =
    textEncoding.GetString(extractFile archiveName file)

/// get the archive name from an extractor
let getArchiveName = function
    | ZipExtractor x -> x.Name
    | SevenZipExtractor x -> x.FileName
    
/// get the list of files in the archive of an extractor
let listFilesFromExtractor = function
    | ZipExtractor x -> Zip.listFiles x
    | SevenZipExtractor x -> SevenZip.listFiles x
    
/// get the list of files in an archive
let listFiles archive =
    use extractor = openArchive archive
    listFilesFromExtractor extractor
    
/// create an archive, add a sequence of stream/entry name pairs
let createArchive = Zip.createArchive

// helper functions for use with createArchive
let addStream (files: ResizeArray<#Stream * _>) stream entryName = files.Add (stream, entryName)
let addFile files filePath entryName = addStream files (File.OpenRead filePath :> Stream) entryName
let addTextFile files (text: string) entryName = addStream files (new MemoryStream(textEncoding.GetBytes text) :> Stream) entryName
let addDirectory files path =
     Directory.GetFiles(path, "*",  SearchOption.AllDirectories) 
     |> Array.iter (fun p -> p.Substring (path.Length + 1) |> addFile files p)

  
module Types =
    /// represents a file in an archive, for use in the VFS
    type ArchiveFileData =
        abstract Stream: Stream
        abstract Text: string
        abstract ArchiveName: string
        abstract FileName: string
        abstract Bytes: byte[]
    /// represents a file in an archive, data is pre-loaded
    type FileData (bytes, text, fileName, archiveName) =
        interface ArchiveFileData with
            override this.Text = text
            override this.ArchiveName = archiveName
            override this.FileName = fileName
            override this.Bytes = bytes
            override this.Stream = new MemoryStream(bytes) :> Stream
    /// represents a file in an archive, data is loaded only when needed and cached
    type LazyFileData (extractor: Extractor, fileName: string) =
        let archiveName = getArchiveName extractor
        let bytes = lazy extractFileWithExtractor fileName extractor
        let text = lazy textEncoding.GetString bytes.Value    
        interface ArchiveFileData with
            override this.Text = text.Value
            override this.ArchiveName = archiveName
            override this.FileName = fileName
            override this.Bytes = bytes.Value
            override this.Stream = new MemoryStream(bytes.Value) :> Stream