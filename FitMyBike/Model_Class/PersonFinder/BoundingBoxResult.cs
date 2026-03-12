using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitMyBike.Model_Class.PersonFinder
{
    internal class BoundingBoxResult
    {
        public string Label { get; set; }
        public Rect2f BoundingBox { get; set; }

        public double Score { get; set; }   

    }
}
