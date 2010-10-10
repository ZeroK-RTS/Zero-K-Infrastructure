#light

open System.Net.Sockets
open System.IO

let test () =
    use client = new TcpClient() 
    printfn "connecting"
    client.Connect("planet-wars.eu", 2666)
    use stream = client.GetStream ()
    use out = new StreamWriter(stream, AutoFlush = true)
    use inp = new StreamReader(stream)
    printfn "sending"
    out.WriteLine "/getranks SirFaust NoruasMan robert"
    printfn "response: %s" <| inp.ReadLine ()

test ()
