using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitMyBike.Model_Class.PoseEstimator
{
    public class KeypointResult
    {
        public string Alias { get; set; }
        public int KeypointIndex { get; set; }
        public Point Point { get; set; }
        public Point RawPoint { get; set; }

        public Point? CalculateVectorToAnotherKeypointResult(KeypointResult keypointResult)
        {
            Point? point = null;

            if (keypointResult != null)
            {
                point = new Point(this.Point.X - keypointResult.Point.X, this.Point.Y - keypointResult.Point.Y);
            }

            return point;
        }
    }
}
