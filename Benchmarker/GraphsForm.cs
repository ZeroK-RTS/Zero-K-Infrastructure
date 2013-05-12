using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using ZedGraph;

namespace Benchmarker
{
    public partial class GraphsForm: Form
    {
        readonly BatchRunResult results;

        public GraphsForm(BatchRunResult results) {
            this.results = results;
            InitializeComponent();
        }

        static List<Color> Colors = new List<Color>() { Color.Blue, Color.Red, Color.Green, Color.Orange, Color.Black, Color.Cyan, Color.BlueViolet, Color.Magenta, Color.Gray, Color.Yellow, Color.Pink };

        void GraphsForm_Load(object sender, EventArgs e) {
            
            Dictionary<Tuple<string, string>, ZedGraphControl> charts = new Dictionary<Tuple<string, string>, ZedGraphControl>();


            foreach (var res in results.RunEntries) {
                foreach (var grp in res.RawValues.GroupBy(x => x.Key).Where(x => x.Count() > 2)) {
                    ZedGraphControl graph;
                    if (!charts.TryGetValue(Tuple.Create(res.Benchmark.Name, grp.Key), out graph)) {
                        graph = new ZedGraphControl();
                        charts[Tuple.Create(res.Benchmark.Name, grp.Key)] = graph;
                        var pane = graph.GraphPane;
                        pane.XAxis.Title.Text = "gameframe";
                        pane.YAxis.Title.Text = grp.Key;
                        pane.Title.Text = string.Format("{1} {0}", res.Benchmark, grp.Key);
                        pane.Chart.Fill = new Fill(Color.White, Color.LightGray, 45.0f);
                        
                        pane.XAxis.Scale.MaxAuto = true;
                        pane.XAxis.Scale.MinAuto = true;
                        pane.YAxis.Scale.MaxAuto = true;
                        pane.YAxis.Scale.MinAuto = true;

                        pane.XAxis.Scale.MaxGrace = 0;
                        pane.YAxis.Scale.MaxGrace = 0;


                        graph.Width = Width - 40;
                        graph.Height = Width / 4;

                        graph.IsShowPointValues = true;
                        flowPanel.Controls.Add(graph);

                    }

                    var curve = graph.GraphPane.AddCurve(res.TestCase.ToString(),
                                             grp.Select(x => x.GameFrame).ToArray(),
                                             grp.Select(x => x.Value).ToArray(),
                                             Colors[graph.GraphPane.CurveList.Count%Colors.Count], SymbolType.None);
                    
                    curve.Line.Style= DashStyle.Solid;
                    curve.Line.Width = 2;
                }
            }

            foreach (var graph in charts.Values) {
                graph.AxisChange();
                graph.Invalidate();
            }

            if (flowPanel.Controls.Count == 0) Close(); // no graphs
        }
    }
}