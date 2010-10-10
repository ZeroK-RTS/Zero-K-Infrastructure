#light

#light

open System
open System.Windows.Forms
open System.Windows.Forms.DataVisualization.Charting
open System.Drawing
open Data
open PlanetWarsShared.Events

let getChartFormat () =
    let chart = new Chart(Dock = DockStyle.Fill, BorderlineDashStyle = ChartDashStyle.Solid, BorderlineWidth = 2)
    let area = new ChartArea()
    area.AxisX.IsMarginVisible <- true
    area.AxisX.MajorGrid.Enabled <- false
    area.AxisY.MajorGrid.Enabled <- false
    area.BackColor <- Color.White
    chart.ChartAreas.Add area
    chart
    
let getChart seriesChartType =

    let chart = getChartFormat ()

    //let toDay (date: DateTime) = date.DayOfYear - (Seq.hd turns).Date.DayOfYear |> float
    
    let addFaction faction color = 
        let series = new Series(ChartType = seriesChartType, XValueType = ChartValueType.DateTime, Color = color, BorderWidth = 3)
        chart.Series.Add series

        for planets, date in factionPlanetsByDate faction do
            series.Points.AddXY(date.ToOADate (), float planets) |> ignore
        
    addFaction Arm Color.Blue
    addFaction Core Color.Red
    chart

let getTabPage () = 
    
    let tabPage = new TabPage(Text = "Planet Chart")
    let chart = getChart SeriesChartType.Line
        
    let duelSeries = new Series(ChartType = SeriesChartType.Column, XValueType = ChartValueType.DateTime, Color = Color.LightBlue, BorderWidth = 3)
    chart.Series.Add duelSeries
    
    for date, freq in duelsByDay do
        duelSeries.Points.AddXY(date.ToOADate (), float freq) |> ignore
    
    tabPage.Controls.Add chart
    tabPage


