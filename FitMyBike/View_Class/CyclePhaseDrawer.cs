using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Shapes;

namespace FitMyBike.View_Class
{
    internal class CyclePhaseDrawer
    {
        int minimumHeight = 10;
        int maximumHeight = 100;

        List<double> lineHeightList = new List<double>();

        public CyclePhaseDrawer(IEnumerable<double> minimaValues, IEnumerable<double> maximaValues, IEnumerable<double> allKneeAngleValues)
        {
            var minimaValuesMedian = minimaValues.OrderBy(x => x).ToArray()[(int)(minimaValues.Count() / 2)];
            var maximaValuesMedian = maximaValues.OrderBy(x => x).ToArray()[(int)(maximaValues.Count() / 2)];
            var spread = maximaValuesMedian - minimaValuesMedian;

            foreach (var kneeAngle in allKneeAngleValues)
            {
                lineHeightList.Add(minimumHeight + ((maximumHeight - minimumHeight) * (maximaValuesMedian - kneeAngle) / spread));
            }
        }

        public void DrawLines(Canvas canvasToDrawOnto)
        {
            var rectangleWidth = canvasToDrawOnto.ActualWidth / (lineHeightList.Count);

            canvasToDrawOnto.Dispatcher.Invoke(new Action(() =>
            {
                System.Windows.Media.Brush _rectangleBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(192, 200, 0, 200));

                for (int i = 0; i < lineHeightList.Count; i++)
                {
                    Line line = new Line();
                    
                    line.X1 = i * (rectangleWidth);
                    line.X2 = i * (rectangleWidth);
                    line.Y1 = canvasToDrawOnto.ActualHeight - lineHeightList[i] > 0 ? canvasToDrawOnto.ActualHeight - lineHeightList[i] : 0;
                    line.Y2 = canvasToDrawOnto.ActualHeight;

                    line.Stroke = _rectangleBrush;
                    line.StrokeThickness = (rectangleWidth);

                    canvasToDrawOnto.Children.Add(line);

                }
            }));
        }
    }
}
