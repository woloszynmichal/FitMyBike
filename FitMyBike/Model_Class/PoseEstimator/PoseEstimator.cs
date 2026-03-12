using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.ML.OnnxRuntime;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Numerics;
using FitMyBike.Model_Class.PersonFinder;
using FitMyBike.Model_Class.Enums;

namespace FitMyBike.Model_Class.PoseEstimator
{
    internal class PoseEstimator_B : IDisposable
    {
        string modelPath = @"DL_Models\keypoints_finder.onnx";
        InferenceSession _session = null;

        const int TargetHeight = 256;
        const int TargetWidth = 192;
        static readonly float[] Mean = { 0.485f, 0.456f, 0.406f };
        static readonly float[] Std = { 0.229f, 0.224f, 0.225f };
        const float RescaleFactor = 1f / 255f;
        const float NormalizeFactor = 200f;

        static Dictionary<int, string> id2label = new Dictionary<int, string>
            {
                { 0, "Nose" }, { 1, "L_Eye" }, { 2, "R_Eye" }, { 3, "L_Ear" }, { 4, "R_Ear" },
                { 5, "L_Shoulder" }, { 6, "R_Shoulder" }, { 7, "L_Elbow" }, { 8, "R_Elbow" },
                { 9, "L_Wrist" }, { 10, "R_Wrist" }, { 11, "L_Hip" }, { 12, "R_Hip" },
                { 13, "L_Knee" }, { 14, "R_Knee" }, { 15, "L_Ankle" }, { 16, "R_Ankle" }
            };

        static List<int> _rightSideLabelList = new List<int>() { 4, 6, 8, 10, 12, 14, 16 };
        static List<int> _leftSideLabelList = new List<int>() { 3, 5, 7, 9, 11, 13, 15 };

        ~PoseEstimator_B()
        {
            Dispose(false);
        }

        public void CreateSessionAndTensors()
        {
            var options = new SessionOptions();
            options.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;
            _session = new InferenceSession(modelPath, options);
        }

        public void EstimatePose(Mat imageInput, BoundingBoxResult personBoundingBox, SelectedSide selectedSide, out List<KeypointResult> keypoints, out Dictionary<PoseAngles, double> poseAngleDict, out Dictionary<PoseKeypoint, KeypointResult> drawKeypoints)
        {
            keypoints = new List<KeypointResult>();
            drawKeypoints = new Dictionary<PoseKeypoint, KeypointResult>();
            poseAngleDict = new Dictionary<PoseAngles, double>();

            foreach (var enumElement in Enum.GetValues(typeof(PoseAngles)))
            {
                PoseAngles poseAngle = (PoseAngles)enumElement;
                poseAngleDict[poseAngle] = 0;
            }

            using (Mat image = new Mat(imageInput, new Rect()
                {
                    X = (int)Math.Floor(personBoundingBox.BoundingBox.X),
                    Y = (int)Math.Floor(personBoundingBox.BoundingBox.Y),
                    Width = (int)Math.Floor(personBoundingBox.BoundingBox.Width),
                    Height = (int)Math.Floor(personBoundingBox.BoundingBox.Height),
                }
            ))
            {
                var imageWidthScale = (float)image.Width / TargetWidth;
                var imageHeightScale = (float)image.Height / TargetHeight;

                using (Mat resized = new Mat())
                using (Mat floatImage = new Mat())
                {
                    Cv2.Resize(image, resized, new Size(TargetWidth, TargetHeight));

                    // Convert to float and normalize
                    resized.ConvertTo(floatImage, MatType.CV_32FC3, RescaleFactor);

                    // Split channels
                    Mat[] channels = Cv2.Split(floatImage);
                    for (int i = 0; i < 3; i++)
                    {
                        channels[i] = (channels[i] - Mean[i]) / Std[i];
                    }

                    // Merge back
                    using (Mat normalized = new Mat())
                    {
                        Cv2.Merge(channels, normalized);

                        // Convert to tensor
                        var inputTensor = new DenseTensor<float>(new[] { 1, 3, TargetHeight, TargetWidth });
                        for (int c = 0; c < 3; c++)
                        {
                            for (int h = 0; h < TargetHeight; h++)
                            {
                                for (int w = 0; w < TargetWidth; w++)
                                {
                                    inputTensor[0, c, h, w] = normalized.At<Vec3f>(h, w)[c];
                                }
                            }
                        }

                        // Run inference
                        var inputs = new List<NamedOnnxValue>
                        {
                            NamedOnnxValue.CreateFromTensor("pixel_values", inputTensor)
                        };

                        using (IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results = _session.Run(inputs))
                        {
                            var output = results.First().AsTensor<float>().AsEnumerable<float>().ToArray();

                            // Interpret output
                            int numJoints = 17;
                            int heatmapHeight = TargetHeight / 4;
                            int heatmapWidth = TargetWidth / 4;

                            for (int joint = 0; joint < numJoints; joint++)
                            {
                                float maxVal = float.MinValue;
                                int maxX = 0, maxY = 0;

                                for (int y = 0; y < heatmapHeight; y++)
                                {
                                    for (int x = 0; x < heatmapWidth; x++)
                                    {
                                        float val = output[0 * numJoints * heatmapHeight * heatmapWidth +
                                                            joint * heatmapHeight * heatmapWidth +
                                                            y * heatmapWidth + x];

                                        if (val > maxVal)
                                        {
                                            maxVal = val;
                                            maxX = x;
                                            maxY = y;
                                        }
                                    }
                                }

                                // Scale back to original image size
                                int finalX = (int)(maxX * 4);
                                int finalY = (int)(maxY * 4);
                                //Debug.WriteLine($"Joint {id2label[joint]}: ({finalX}, {finalY})");
                                keypoints.Add(
                                    new KeypointResult() 
                                    { 
                                        KeypointIndex = joint, 
                                        Alias = id2label[joint], 
                                        RawPoint = new Point(finalX, finalY), 
                                        Point = new Point(personBoundingBox.BoundingBox.X + (finalX * imageWidthScale), personBoundingBox.BoundingBox.Y + (finalY * imageHeightScale)) 
                                    });
                            }
                        }

                        var indexListFromSelectedSide = selectedSide == SelectedSide.Right ? _rightSideLabelList : _leftSideLabelList;


                        DrawKeypoints(
                                ref imageInput, 
                                keypoints, 
                                new Point(personBoundingBox.BoundingBox.X, personBoundingBox.BoundingBox.Y), 
                                imageWidthScale, 
                                imageHeightScale,
                                indexListFromSelectedSide
                            );
                        GetTheAngles(
                                keypoints, 
                                new Point(personBoundingBox.BoundingBox.X, personBoundingBox.BoundingBox.Y), 
                                imageWidthScale, 
                                imageHeightScale,
                                indexListFromSelectedSide, 
                                ref poseAngleDict
                            );

                        foreach( var selectedIndex in indexListFromSelectedSide)
                        {
                            drawKeypoints.Add((PoseKeypoint)indexListFromSelectedSide.IndexOf(selectedIndex), keypoints[selectedIndex]);
                        }
                    }

                    for (int channgelNumber = 0; channgelNumber < channels.Length; channgelNumber++)
                    {
                        channels[channgelNumber]?.Dispose();
                    }
                }
            }
        }

        private static void GetTheAngles(List<KeypointResult> keypoints, Point point, float imageWidthScale, float imageHeightScale, List<int> selectedSide, ref Dictionary<PoseAngles, double> poseAngleDict)
        {
            var earPoint = keypoints.Where(x => x.KeypointIndex == selectedSide[0]).Any() ? keypoints.Where(x => x.KeypointIndex == selectedSide[0]).First() : null;
            var shoulderPoint = keypoints.Where(x => x.KeypointIndex == selectedSide[1]).Any() ? keypoints.Where(x => x.KeypointIndex == selectedSide[1]).First() : null;
            var elbowPoint = keypoints.Where(x => x.KeypointIndex == selectedSide[2]).Any() ? keypoints.Where(x => x.KeypointIndex == selectedSide[2]).First() : null;
            var wristPoint = keypoints.Where(x => x.KeypointIndex == selectedSide[3]).Any() ? keypoints.Where(x => x.KeypointIndex == selectedSide[3]).First() : null;
            var hipPoint = keypoints.Where(x => x.KeypointIndex == selectedSide[4]).Any() ? keypoints.Where(x => x.KeypointIndex == selectedSide[4]).First() : null;
            var kneePoint = keypoints.Where(x => x.KeypointIndex == selectedSide[5]).Any() ? keypoints.Where(x => x.KeypointIndex == selectedSide[5]).First() : null;
            var anklePoint = keypoints.Where(x => x.KeypointIndex == selectedSide[6]).Any() ? keypoints.Where(x => x.KeypointIndex == selectedSide[6]).First() : null;

            var vectorHipKnee = hipPoint?.CalculateVectorToAnotherKeypointResult(kneePoint);
            var vectorAnkleKnee = anklePoint?.CalculateVectorToAnotherKeypointResult(kneePoint);
            var vectorKneeHip = kneePoint?.CalculateVectorToAnotherKeypointResult(hipPoint);
            var vectorShoulderHip = shoulderPoint?.CalculateVectorToAnotherKeypointResult(hipPoint);
            var vectorHipShoulder = hipPoint?.CalculateVectorToAnotherKeypointResult(shoulderPoint);
            var vectorElbowShoulder = elbowPoint?.CalculateVectorToAnotherKeypointResult(shoulderPoint);
            var vectorWristElbow = wristPoint?.CalculateVectorToAnotherKeypointResult(elbowPoint);
            var vectorShoulderElbow = shoulderPoint?.CalculateVectorToAnotherKeypointResult(elbowPoint);
            var vectorWristShoulder = wristPoint?.CalculateVectorToAnotherKeypointResult(shoulderPoint);

            var kneeAngle = GetAngleFromVectors(vectorHipKnee, vectorAnkleKnee);
            var backAngle = GetAngleFromVectors(vectorShoulderHip, vectorKneeHip);
            var armAngle = GetAngleFromVectors(vectorHipShoulder, vectorElbowShoulder);
            var elbowAngle = GetAngleFromVectors(vectorWristElbow, vectorShoulderElbow);
            var wholeArmAngle = GetAngleFromVectors(vectorHipShoulder, vectorWristShoulder);

            poseAngleDict[PoseAngles.KneeAngle] = kneeAngle;
            poseAngleDict[PoseAngles.BackAngle] = backAngle;
            poseAngleDict[PoseAngles.ArmAngle] = armAngle;
            poseAngleDict[PoseAngles.ElbowAngle] = elbowAngle;
            poseAngleDict[PoseAngles.WholeArmAngle] = wholeArmAngle;
        }

        public static double GetAngle(Point2f hip, Point2f knee, Point2f ankle)
        {
            var v1 = new Point2f(hip.X - knee.X, hip.Y - knee.Y);
            var v2 = new Point2f(ankle.X - knee.X, ankle.Y - knee.Y);

            double dotProduct = v1.X * v2.X + v1.Y * v2.Y;
            double magnitude1 = Math.Sqrt(v1.X * v1.X + v1.Y * v1.Y);
            double magnitude2 = Math.Sqrt(v2.X * v2.X + v2.Y * v2.Y);

            double angleRad = Math.Acos(dotProduct / (magnitude1 * magnitude2));

            return angleRad * (180.0 / Math.PI);
        }

        static double GetAngleFromVectors(Point? vectorAorNull, Point? vectorBorNull)
        {
            if (vectorAorNull == null || vectorBorNull == null)
            {
                return -1;
            }
            else
            {
                Point vectorA = (Point)vectorAorNull;
                Point vectorB = (Point)vectorBorNull;
                var dot = vectorA.X * vectorB.X + vectorA.Y * vectorB.Y;
                var mag_vectorA = Math.Sqrt(Math.Pow(vectorA.X, 2) + Math.Pow(vectorA.Y, 2));
                var mag_vectorB = Math.Sqrt(Math.Pow(vectorB.X, 2) + Math.Pow(vectorB.Y, 2));

                return (180.0 * Math.Acos(dot / (mag_vectorA * mag_vectorB))) / Math.PI;
            }
        }

        static void DrawKeypoints(ref Mat originalImage, List<KeypointResult> keypoints, Point topLeftCornerOfBoundingBox, float imageWidthScale, float imageHeightScale, List<int> selectedSide)
        {
            for (int i = 0; i < keypoints.Count; i++)
            {
                if (selectedSide.Contains(keypoints[i].KeypointIndex))
                {
                    Cv2.Circle(originalImage, keypoints[i].Point, 5, Scalar.Red, -1);
                    Cv2.PutText(originalImage, keypoints[i].Alias.ToString(), keypoints[i].Point + new Point(5, -5), HersheyFonts.HersheySimplex, 0.5, Scalar.Yellow, 1);
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            _session?.Dispose();
            if (disposing)
            {
                // release other disposable objects

            }
        }
    }
}
