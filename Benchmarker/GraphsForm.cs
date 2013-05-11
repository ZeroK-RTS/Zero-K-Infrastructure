using System;
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
            foreach (var res in results.RunEntries) {
                foreach (var grp in res.RawValues.GroupBy(x => x.Key).Where(x => x.Count() > 2)) {
                    var graph = new Chart();
                    var area = new ChartArea();
                    area.AxisX.Title = "gameframe";
                    area.AxisY.Title = grp.Key;
                    graph.Titles.Add(string.Format("{0} {1}", res.Benchmark, res.TestCase));
                    graph.BackGradientStyle = GradientStyle.DiagonalLeft;
                    graph.BackSecondaryColor = Color.Silver;
                    graph.ChartAreas.Add(area);

                    var ser = new Series(grp.Key);
                    ser.Color = Color.Red;
                    ser.ChartType = SeriesChartType.Line;

                    graph.Series.Add(ser);

                    graph.Legends.Clear();
                    graph.Width = Width - 40;
                    graph.Height = Width/4;
                    graph.AutoSize = true;

                    foreach (var val in grp) ser.Points.Add(val.Value, val.GameFrame);
                    flowPanel.Controls.Add(graph);
                }
            }
            if (flowPanel.Controls.Count == 0) Close(); // no graphs
        }
    }
}