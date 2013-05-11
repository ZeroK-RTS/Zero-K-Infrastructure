using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace Benchmarker
{
    public partial class GraphsForm: Form
    {
        readonly BatchRunResult results;

        public GraphsForm(BatchRunResult results) {
            this.results = results;
            InitializeComponent();
        }

        void GraphsForm_Load(object sender, EventArgs e) {
            
            Dictionary<Tuple<string, string>, Chart> charts = new Dictionary<Tuple<string, string>, Chart>();

            foreach (var res in results.RunEntries) {
                foreach (var grp in res.RawValues.GroupBy(x => x.Key).Where(x => x.Count() > 2)) {
                    Chart graph;
                    if (!charts.TryGetValue(Tuple.Create(res.Benchmark.Name, grp.Key), out graph)) {
                        graph = new Chart();
                        charts[Tuple.Create(res.Benchmark.Name, grp.Key)] = graph;
                        var area = new ChartArea();
                        area.AxisX.Title = "gameframe";
                        area.AxisY.Title = grp.Key;
                        graph.Titles.Add(string.Format("{1} {0}", res.Benchmark, grp.Key));
                        graph.BackGradientStyle = GradientStyle.DiagonalLeft;
                        graph.BackSecondaryColor = Color.Silver;
                        graph.ChartAreas.Add(area);
                        graph.Width = Width - 40;
                        graph.Height = Width / 4;
                        graph.AutoSize = true;
                        graph.Legends.Add(new Legend("default"));
                        flowPanel.Controls.Add(graph);
                    }

                    var ser = new Series(res.TestCase.ToString());
                    ser.ChartType = SeriesChartType.Line;
                    graph.Series.Add(ser);

                    foreach (var val in grp) ser.Points.Add(val.Value, val.GameFrame);
                    
                }
            }
            if (flowPanel.Controls.Count == 0) Close(); // no graphs
        }
    }
}