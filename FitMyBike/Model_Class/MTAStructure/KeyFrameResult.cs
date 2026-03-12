using FitMyBike.Model_Class.Enums;
using FitMyBike.Model_Class.ExtensionMethods;
using FitMyBike.Model_Class.PoseEstimator;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace FitMyBike.Model_Class.MTAStructure
{
    internal class KeyFrameResult
    {
        public Bitmap Bitmap { get; set; }
        public BitmapSource BitmapSource { get => Bitmap.GetBitmapSource(); }
        public List<KeypointResult> KeypointList { get; set; }
        public Dictionary<PoseKeypoint, KeypointResult> DrawKeypointDictionary { get; set; }
        public bool IsLast { get; set; }
    }
}
