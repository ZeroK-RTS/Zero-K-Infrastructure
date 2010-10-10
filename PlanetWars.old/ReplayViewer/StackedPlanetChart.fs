#light

open System
open System.Windows.Forms
open System.Windows.Forms.DataVisualization.Charting
open System.Drawing
open Data
open PlanetWarsShared.Events

let getTabPage () = 
    let tabPage = new TabPage(Text = "Stacked Planet Chart")
    tabPage.Controls.Add (PlanetChart.getChart SeriesChartType.StackedArea100)
    tabPage