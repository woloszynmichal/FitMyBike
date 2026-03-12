using FitMyBike.Model_Class.Enums;
using FitMyBike.Model_Class.PoseEstimator;
using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace FitMyBike.View_Class
{
    internal class LineSketcher
    {
        public class LinePointReferences
        {
            public PoseKeypoint StartPoint { get; set; }
            public PoseKeypoint EndPoint { get; set; }
        }

        private Dictionary<PoseKeypoint, KeypointResult> _keypointResultDict;
        private Dictionary<LinePointReferences, Line> _lines;
        private double widthScale = 0;
        private double heightScale = 0;

        private Line AnkleKneeLine = new Line();
        private Line KneeHipLine = new Line();
        private Line HipShoulderLine = new Line();
        private Line ShoulderElbowLine = new Line();
        private Line ElbowWristLine = new Line();
        private Line ShoulderHeadLine = new Line();
        private Line ShoulderWristLine = new Line();
        private Line HeelToesLine = new Line();

        public double WidthScale
        {
            get => widthScale;
            set
            {
                if (widthScale != value)
                {
                    widthScale = value;
                }
            }
        }

        public double HeightScale
        {
            get => heightScale;
            set
            {
                if (heightScale != value)
                {
                    heightScale = value;
                }
            }
        }

        public Dictionary<PoseKeypoint, KeypointResult> KeypointResultDict
        {
            get => _keypointResultDict;
            set
            {
                if (_keypointResultDict != value)
                {
                    _keypointResultDict = value;
                    UpdateLines();
                }
            }
        }

        private void UpdateLines()
        {
            foreach (var line in _lines)
            {
                line.Value.X1 = _keypointResultDict[line.Key.StartPoint].Point.X * widthScale;
                line.Value.Y1 = _keypointResultDict[line.Key.StartPoint].Point.Y * heightScale;
                line.Value.X2 = _keypointResultDict[line.Key.EndPoint].Point.X * widthScale;
                line.Value.Y2 = _keypointResultDict[line.Key.EndPoint].Point.Y * heightScale;
            }
        }

        public LineSketcher()
        {
            _lines = new Dictionary<LinePointReferences, Line>()
            {
                {new LinePointReferences() {StartPoint = PoseKeypoint.Ankle, EndPoint = PoseKeypoint.Knee }, AnkleKneeLine },
                {new LinePointReferences() {StartPoint = PoseKeypoint.Knee, EndPoint = PoseKeypoint.Hip }, KneeHipLine },
                {new LinePointReferences() {StartPoint = PoseKeypoint.Hip, EndPoint = PoseKeypoint.Shoulder }, HipShoulderLine },
                {new LinePointReferences() {StartPoint = PoseKeypoint.Shoulder, EndPoint = PoseKeypoint.Elbow }, ShoulderElbowLine },
                {new LinePointReferences() {StartPoint = PoseKeypoint.Elbow, EndPoint = PoseKeypoint.Wrist }, ElbowWristLine },
                {new LinePointReferences() {StartPoint = PoseKeypoint.Shoulder, EndPoint = PoseKeypoint.Ear }, ShoulderHeadLine },
                {new LinePointReferences() {StartPoint = PoseKeypoint.Shoulder, EndPoint = PoseKeypoint.Wrist }, ShoulderWristLine },
                {new LinePointReferences() {StartPoint = PoseKeypoint.Heel, EndPoint = PoseKeypoint.Toes }, HeelToesLine }
            };

            foreach (var line in _lines.Values)
            {
                line.Stroke = new SolidColorBrush(Color.FromArgb(192, 255, 128, 0));
                line.StrokeThickness = 5;
            }
        }

        public List<Line> GetListOfLines()
        {
            return _lines.Values.ToList();
        }

        public PoseKeypoint? LocateClosestPoint(System.Windows.Point point, TransformGroup transformGroup)
        {
            var minLenght = 999999.0;
            PoseKeypoint? closestPoseKeypoint = null;

            var pointTransformed = transformGroup.Transform(point);

            foreach (var lineKeys in _keypointResultDict.Keys)
            {
                var vectorLenght = Math.Sqrt(Math.Pow(_keypointResultDict[lineKeys].Point.X - (point.X / widthScale), 2) + Math.Pow(_keypointResultDict[lineKeys].Point.Y - (point.Y / heightScale), 2));
                if (vectorLenght < minLenght)
                {
                    closestPoseKeypoint = lineKeys;
                    minLenght = vectorLenght;
                }
            }

            return closestPoseKeypoint;
        }

        internal void UpdateLineConnectedToSelectedKeypoint(PoseKeypoint? closestPoseKeypoint, Point point)
        {
            if (closestPoseKeypoint != null)
            {
                KeypointResultDict[(PoseKeypoint)closestPoseKeypoint].Point = new OpenCvSharp.Point(point.X / widthScale, point.Y / heightScale);
                UpdateLines();
            }
            
        }
    }
}
