#light

open System
open System.Windows.Forms
open PWDataLib
open System.Drawing
open PlanetWarsShared
open System.Linq

let galaxy =
    let stateFile = @"C:\Documents and Settings\Administrator\Desktop\serverstate.xml"
    let state = PlanetWarsServer.ServerState.FromFile stateFile
    state.Galaxy
   

let form = new Form(Size = Size(700, 700))
let panel = new FlowLayoutPanel(FlowDirection = FlowDirection.LeftToRight, Dock = DockStyle.Fill, AutoScroll = true)
form.Controls.Add panel
let charts = PWCharts(galaxy)
let addChart url = 
    panel.Controls.Add <| new PictureBox(ImageLocation = url, SizeMode = PictureBoxSizeMode.AutoSize, Dock = DockStyle.Fill)
Array.iter addChart charts.Charts
//addChart (charts.Charts.Last())
    
//panel.Controls.Add <| new TextBox(Text = charts.Captures)


[<STAThread>]
Application.Run form