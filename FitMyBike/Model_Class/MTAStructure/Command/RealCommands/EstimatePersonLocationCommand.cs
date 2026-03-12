using FitMyBike.Model_Class.Enums;
using FitMyBike.Model_Class.PersonFinder;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitMyBike.Model_Class.MTAStructure.Command.RealCommands
{
    internal class EstimatePersonLocationCommand : CommandBaseClass
    {
        private static BoundingBoxResult BoundingBoxFromFirstFrame { get; set; }
        public FitMyBike.Model_Class.PoseEstimator.PoseEstimator PoseEstimator { get; set; }
        public FitMyBike.Model_Class.PersonFinder.PersonFinder PersonFinder { get; set; }
        public KeyframeDictionaryClass KeyframeDictionaryClass { get; set; }
        public Mat ImageFrame { get; set; }
        public bool IsLast { get; set; }

        public override void Dispose()
        {
            PoseEstimator = null;
            PersonFinder = null;
            KeyframeDictionaryClass = null;
            ImageFrame = null;
        }

        public override void DoAction()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            if (KeyFrame == 0) BoundingBoxFromFirstFrame = null;

            if (BoundingBoxFromFirstFrame == null)
            {
                var boundingBoxes = PersonFinder.LocatePerson(ImageFrame);
                if (boundingBoxes.Where(x => x.Label.Contains("person")).Any())
                {
                    var boundingBoxForPersonWithHighestScore = boundingBoxes.Where(x => x.Label.Contains("person")).OrderBy(x => x.Score).First();

                    BoundingBoxFromFirstFrame = new BoundingBoxResult()
                    {
                        BoundingBox = new Rect2f()
                        {
                            X = boundingBoxForPersonWithHighestScore.BoundingBox.X * 0.9f,
                            Y = boundingBoxForPersonWithHighestScore.BoundingBox.Y * 0.9f,
                            Width = boundingBoxForPersonWithHighestScore.BoundingBox.Width * 1.1f,
                            Height = boundingBoxForPersonWithHighestScore.BoundingBox.Height * 1.2f,
                        },
                        Label = boundingBoxForPersonWithHighestScore.Label,
                        Score = -1,
                    };
              
                } 
            }

            EstimatePersonPoseCommand estimatePersonPoseCommand = new EstimatePersonPoseCommand()
            {
                PoseEstimator = this.PoseEstimator,
                KeyframeDictionaryClass = this.KeyframeDictionaryClass,
                PersonBoundingBox = BoundingBoxFromFirstFrame,
                ImageFrame = this.ImageFrame,
                KeyFrame = this.KeyFrame,
                IsLast = this.IsLast              
            };
            estimatePersonPoseCommand.EnqueueThisCommand();

            sw.Stop();
            Debug.WriteLine(nameof(EstimatePersonLocationCommand) + " " + sw.ElapsedMilliseconds);
        }
    }
}
