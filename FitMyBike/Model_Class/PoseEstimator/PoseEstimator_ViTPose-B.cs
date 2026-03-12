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
    internal class PoseEstimator : IDisposable
    {
        string modelPath = @"DL_Models\vitpose-s-wholebody.onnx";
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
                { 13, "L_Knee" }, { 14, "R_Knee" }, { 15, "L_Ankle" }, { 16, "R_Ankle" },
                { 17, "L_FootToes" }, { 18, "L_FootBox" }, { 19, "L_FootHeel" }, { 20, "R_FootToes" },
                { 21, "R_FootBox" }, { 22, "R_FootHeel" }
            };

        static List<int> _rightSideLabelList = new List<int>() { 4, 6, 8, 10, 12, 14, 16, 22, 21 }; 
        static List<int> _leftSideLabelList = new List<int>() { 3, 5, 7, 9, 11, 13, 15, 19, 18 };

        ~PoseEstimator()
        {
            Dispose(false);
        }

        public void CreateSessionAndTensors()
        {
            var options = new SessionOptions();
            options.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;
            _session = new InferenceSession(modelPath, options);
        }

        public void EstimatePose(Mat imageInput, BoundingBoxResult personBoundingBox, out List<KeypointResult> keypoints, out Dictionary<PoseKeypoint, KeypointResult> drawKeypoints)
        {
            keypoints = new List<KeypointResult>();
            drawKeypoints = new Dictionary<PoseKeypoint, KeypointResult>();

            Rect ROI = new Rect()
            {
                X = (int)Math.Floor(personBoundingBox.BoundingBox.X) > 0 ? (int)Math.Floor(personBoundingBox.BoundingBox.X) : 0,
                Y = (int)Math.Floor(personBoundingBox.BoundingBox.Y) > 0 ? (int)Math.Floor(personBoundingBox.BoundingBox.Y) : 0,
                Width = (int)Math.Floor(personBoundingBox.BoundingBox.Width) + personBoundingBox.BoundingBox.X > imageInput.Width ? imageInput.Width - (int)personBoundingBox.BoundingBox.X : (int)Math.Floor(personBoundingBox.BoundingBox.Width),
                Height = (int)Math.Floor(personBoundingBox.BoundingBox.Height) + personBoundingBox.BoundingBox.Y > imageInput.Height ? imageInput.Height - (int)personBoundingBox.BoundingBox.Y : (int)Math.Floor(personBoundingBox.BoundingBox.Height),
            };

            using (Mat image = new Mat(imageInput, ROI))
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
                            NamedOnnxValue.CreateFromTensor("input_0", inputTensor)
                        };

                        using (IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results = _session.Run(inputs))
                        {
                            var output = results.First().AsTensor<float>().AsEnumerable<float>().ToArray();

                            // Interpret output
                            int numJoints = id2label.Count;
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

                        var earsAverageLocationOnAxis_X = keypoints.Where(x => x.Alias.Contains("Ear")).Select(y => y.Point.X).Average();
                        var hipAverageLocationOnAxis_X = keypoints.Where(x => x.Alias.Contains("Hip")).Select(y => y.Point.X).Average();

                        var autoSelectSide = earsAverageLocationOnAxis_X > hipAverageLocationOnAxis_X ? SelectedSide.Right : SelectedSide.Left;
                        var indexListFromSelectedSide = autoSelectSide == SelectedSide.Right ? _rightSideLabelList : _leftSideLabelList;

                        //DrawKeypoints(
                        //        ref imageInput, 
                        //        keypoints, 
                        //        new Point(personBoundingBox.BoundingBox.X, personBoundingBox.BoundingBox.Y), 
                        //        imageWidthScale, 
                        //        imageHeightScale,
                        //        indexListFromSelectedSide
                        //    );

                        foreach ( var selectedIndex in indexListFromSelectedSide)
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
