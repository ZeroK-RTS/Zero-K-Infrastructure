#light

namespace PWDataLib

open System
open System.Drawing
open GoogleChartSharp
open PWData

type ChartInfo = 
    { DataSet :  Map<Ownership, (DateTime * int) list>;
      Legend : string list;
      Colors : string list;
      Style : string }

type PWCharts (galaxy: PlanetWarsShared.Galaxy) =
    
    let data = PWData(galaxy)
    
    let setGranularity datapointCount datapoints =
        let datapoints = List.sort_by fst datapoints
        let xValues = List.map fst datapoints
        let minX = List.min xValues
        let maxX = List.max xValues
        let percentile n = List.find (fun (x, _) -> x >= ((maxX - minX) * n) + minX) datapoints
        let data = [0..datapointCount] |> List.map (fun n -> float n / (float datapointCount) |> percentile)
        data
          
    let makeStandardChart title size (chartInfos: ChartInfo list) =

        let lineChart = LineChart(fst size, snd size)
        let getChartData chartInfo = 
            let getData faction = 
                let line1x, line1y =  
                    chartInfo.DataSet.[faction] 
                    |> List.map (fun (date, captures) -> date.ToOADate (), captures)
                    |> setGranularity 50
                    |> Array.of_list
                    |> Array.unzip
                line1y
            [Arm; Core] |> List.map getData
        let mapConcatArray f = chartInfos |> List.map_concat f |> Array.of_list
        let dataSet = (mapConcatArray getChartData)
        let maxY = dataSet |> Array.map Array.max |> Array.max
        let normalizedDataSet = dataSet |> Array.map (Array.map (fun y -> y * 4095 / maxY))
        lineChart.SetData normalizedDataSet
        lineChart.SetTitle title
        lineChart.SetDatasetColors (mapConcatArray (fun c -> c.Colors))
        let axis = ChartAxis(ChartAxisType.Left)
        axis.SetRange (0, maxY)
        lineChart.AddAxis axis
        lineChart.SetLegend (mapConcatArray (fun c -> c.Legend))
        let styles = String.Join ("|", chartInfos |> List.map_concat (fun c -> [c.Style; c.Style]) |> Array.of_list)
        lineChart.GetUrl () + (* legend pos *) "&chdlp=b" + (* chart style *) "&chls=" + styles
        
    let captures = 
        makeStandardChart "Planets Captured" (300, 300)
               [ { DataSet = data.Captures
                   Legend = [ "Arm Encirclements"; "Core Encirclements" ]
                   Colors = [ "33CCFF"; "FFABAB" ]
                   Style = "1,6,3" } 
                 { DataSet = data.VictoriesNo1v1
                   Legend = [ "Arm Victories (no 1v1)"; "Core Victories (no 1v1)"]
                   Colors = [ "0000FF"; "FF0000" ]
                   Style = "3,1,0" }
                 { DataSet = data.Victories1v1
                   Legend = [ "Arm Victories 1v1"; "Core Victories 1v1"]
                   Colors = [ "0D038F"; "8F0303" ]
                   Style = "1,1,0" } ]
                   
    let victories =
        makeStandardChart "Victories" (300, 200)
            [ { DataSet = data.Victories
                Legend = [ "Arm"; "Core" ]
                Colors = [ "0000FF"; "FF0000" ]
                Style = "1,1,0"} ] 
    let planets =
        makeStandardChart "Planets Controlled" (300, 200)
            [ { DataSet = data.Planets
                Legend = [ "Arm"; "Core" ]
                Colors = [ "0000FF"; "FF0000" ]
                Style = "1,1,0"} ] 
                
    let awardHistogramm =
        let max = Array.max (snd data.Awards)
        let normalizedData = Array.map (fun y -> y * 4095 / max) (snd data.Awards)
        let pieChart = BarChart(600, 400, BarChartOrientation.Horizontal, BarChartStyle.Grouped);
        pieChart.SetTitle "Awards"
        pieChart.SetData (normalizedData)
        let axis = new ChartAxis(ChartAxisType.Left, fst data.Awards |> Array.rev)
        pieChart.AddAxis axis
        let axis = new ChartAxis(ChartAxisType.Bottom)
        axis.SetRange (0, max)
        pieChart.AddAxis axis
        pieChart.SetDatasetColors [| "C3D3FF" |]
        pieChart.GetUrl ()
        
    let planetPie =
        let planets = data.CurrentPlanetsControlled
        let pieChart = PieChart(300, 150, PieChartType.ThreeD);
        pieChart.SetTitle "Planets Controlled"
        pieChart.SetData planets
        pieChart.SetPieChartLabels [| sprintf "Arm (%d)" planets.[0]; sprintf "Core (%d)" planets.[1] |];
        pieChart.SetDatasetColors [| "0000FF"; "FF0000" |]
        pieChart.GetUrl ()
        
    let rankPie =
        let pieChart = PieChart(400, 200, PieChartType.TwoD);
        pieChart.SetTitle "Ranks"
        pieChart.SetData (snd data.Ranks)
        pieChart.SetPieChartLabels (fst data.Ranks);
        pieChart.SetDatasetColors [| "0000FF"; "FF0000"; "1FFF00"|]
        pieChart.GetUrl ()
        
    let creditBars =
        let barChart = BarChart(300, 300, BarChartOrientation.Vertical, BarChartStyle.Stacked);
        barChart.SetTitle "Credits"
        barChart.AddAxis <| ChartAxis(ChartAxisType.Bottom, [| "Arm"; "Core" |])
        let max = data.Credits |> (fun a -> [a.[0].[0] + a.[1].[0]; a.[0].[1] + a.[1].[1]] ) |> List.max
        let axis = ChartAxis(ChartAxisType.Left, [| "0"; max / 100000.f |> int |> sprintf "%d Million €" |] )
        barChart.AddAxis axis
        let normalizedDataSet = data.Credits |> Array.map (Array.map (fun y -> y * 100.f / max))
        printfn "%A" normalizedDataSet
        barChart.SetData normalizedDataSet  
        barChart.SetDatasetColors [| "FF0000"; "00FF00" |]
        barChart.SetLegend [| "Credits Spent"; "Credits Available" |]
        barChart.GetUrl () + "&chbh=r,1,1.5"
        
    let players =
        let dataSet = setGranularity 50 data.PlayerCount |> Array.of_list |> Array.unzip |> snd
        let max = Array.max dataSet
        let normalized = Array.map (fun y -> y * 4095 / max) dataSet
        let lineChart = LineChart(250, 150)
        lineChart.SetTitle "Players"
        lineChart.SetData normalized
        let axis = ChartAxis(ChartAxisType.Left)
        axis.SetRange (0, max)
        lineChart.SetDatasetColors [| "0000FF" |]
        lineChart.AddAxis axis
        lineChart.GetUrl ()
        
    let playerPie =
        let pieChart = PieChart(300, 150, PieChartType.ThreeD);
        pieChart.SetTitle "Players"
        pieChart.SetData  data.FactionPlayers 
        pieChart.SetPieChartLabels [| sprintf "Arm (%d)" data.FactionPlayers.[0]; sprintf "Core (%d)" data.FactionPlayers.[1] |];
        pieChart.SetDatasetColors [| "0000FF"; "FF0000" |]
        pieChart.GetUrl ()
        
    // battles per player
    // players who played in the last x days
    // awards / rankpoints
    // rankpoints / battle
    
        
    member x.Charts = [| planetPie; planets; captures; victories; players; playerPie; rankPie; creditBars; awardHistogramm |]
        
    
    