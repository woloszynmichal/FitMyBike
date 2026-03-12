using FitMyBike.Model_Class.Enums;
using FitMyBike.Model_Class.ExtensionMethods;
using FitMyBike.Model_Class.PersonFinder;
using FitMyBike.Model_Class.PoseEstimator;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace FitMyBike.Model_Class.MTAStructure.Command.RealCommands
{
    internal class EstimatePersonPoseCommand : CommandBaseClass
    {
        public FitMyBike.Model_Class.PoseEstimator.PoseEstimator PoseEstimator {  get; set; }
        public KeyframeDictionaryClass KeyframeDictionaryClass { get; set; }
        public BoundingBoxResult PersonBoundingBox { get; set; }
        public Mat ImageFrame { get; set; }
        public bool IsLast { get; set; }


        public override void Dispose()
        {
            PoseEstimator = null;
            KeyframeDictionaryClass = null;
            PersonBoundingBox = null;
            ImageFrame?.Dispose();
            ImageFrame = null;
        }

        public override void DoAction()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            if (PersonBoundingBox != null)
            {
                PoseEstimator.EstimatePose(ImageFrame, PersonBoundingBox, out List<KeypointResult> keypointList, out Dictionary<PoseKeypoint, KeypointResult> keypointDictionary);

                KeyFrameResult keyFrameResult = new KeyFrameResult();
                keyFrameResult.Bitmap = ImageFrame.ToBitmap();
                keyFrameResult.KeypointList = keypointList;
                keyFrameResult.DrawKeypointDictionary = keypointDictionary;
                keyFrameResult.IsLast = this.IsLast;

                KeyframeDictionaryClass.InsertSingleFrameResult(this.KeyFrame, keyFrameResult); 
            }

            sw.Stop();
            Debug.WriteLine(nameof(EstimatePersonPoseCommand) + " " + sw.ElapsedMilliseconds);
        }
    }
}
