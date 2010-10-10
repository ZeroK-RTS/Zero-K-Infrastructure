#light

// This file is a script that can be executed with the F# Interactive.  
// It can be used to explore and test the library project.
// Note that script files will not be part of the project build.

#r "../../bin/PlanetWarsShared.dll"
#r "../../libs/GoogleChartSharp.dll"
#r "../../bin/PlanetWarsServer.exe"
#r "System.Windows.Forms.dll"
#r "System.Drawing.dll"
#r "System.Core.dll"
#r "System.Data.dll"
#r "System.Xml.dll"


#load "PWData.fs"
#load "PWCharts.fs"

open System.Windows.Forms
open PWDataLib
open System.Drawing
open PlanetWarsShared

let galaxy =
    let stateFile = @"C:\Documents and Settings\Administrator\Desktop\serverstate.xml"
    let state = PlanetWarsServer.ServerState.FromFile stateFile
    state.Galaxy
   

let form = new Form(Size = Size(600, 600))
let panel = new FlowLayoutPanel(FlowDirection = FlowDirection.TopDown, Dock = DockStyle.Fill)
form.Controls.Add panel
let charts = PWCharts(galaxy)
panel.Controls.Add <| new PictureBox(ImageLocation = charts.Captures, SizeMode = PictureBoxSizeMode.AutoSize, Dock = DockStyle.Fill)


form.Show()

//let data = List.init 50 ((+) 50) |> List.map float
//let minX = List.min data
//let maxX = List.max data
//
//let percentile n = List.find (fun x -> x > ((maxX - minX) * n) + minX) data
//
//percentile 0.5 |> printfn "%f"