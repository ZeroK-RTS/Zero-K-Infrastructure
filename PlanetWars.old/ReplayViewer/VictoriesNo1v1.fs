#light

open System
open System.Windows.Forms
open System.Windows.Forms.DataVisualization.Charting
open System.Drawing
open Data

let getTabPage () =
    let label = "Victories and Encirclements (No 1v1)"
    let tabPage = new TabPage(label)
    let chart = Victories.getChart label Data.victoriesNo1v1
    tabPage.Controls.Add chart
    
    let addFaction dataFunc faction color borderWidth = 
        let series = new Series(ChartType = SeriesChartType.Line, XValueType = ChartValueType.DateTime, Color = color, BorderWidth = borderWidth)
        chart.Series.Add series
        series.LegendText <- "test"

        for date, planets in (dataFunc: Ownership -> (DateTime * int) list) faction do
            series.Points.AddXY(date.ToOADate (), float planets) |> ignore
            

    addFaction captures Arm Color.LightBlue 3
    addFaction captures Core Color.Pink 3 
    addFaction victories1v1 Arm Color.DarkBlue 1
    addFaction victories1v1 Core Color.DarkRed 1
    
    
    tabPage