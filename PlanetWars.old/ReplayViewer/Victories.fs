#light

open System
open System.Windows.Forms
open System.Windows.Forms.DataVisualization.Charting
open System.Drawing
open Data

let getChart (label: string) (victoriesFunction: Ownership -> (DateTime * int) list) =
    let chart = PlanetChart.getChartFormat ()
    
    chart.Titles.Add label |> ignore
    
    let addFaction faction color = 
        let series = new Series(ChartType = SeriesChartType.Line, XValueType = ChartValueType.DateTime, Color = color, BorderWidth = 3)
        chart.Series.Add series

        for date, planets in victoriesFunction faction do
            series.Points.AddXY(date.ToOADate (), float planets) |> ignore
        
    addFaction Arm Color.Blue
    addFaction Core Color.Red
    chart
    
let getPage label (victoriesFunction: Ownership -> (DateTime * int) list) =
    let page = new TabPage(label)
    page.Controls.Add (getChart label victoriesFunction)
    page
    
let getTabPage () =
    getPage "Victories" victories