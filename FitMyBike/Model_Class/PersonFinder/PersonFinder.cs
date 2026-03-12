using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.ML.OnnxRuntime;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FitMyBike.Properties;
using Microsoft.ML.Data;

namespace FitMyBike.Model_Class.PersonFinder
{
    internal class PersonFinder : IDisposable
    {
        public readonly string modelPath = @"DL_Models\person_locator.onnx";
        InferenceSession _session = null;

        //Data from input model JSON file
        static readonly int targetWidth = 576;
        static readonly int targetHeight = 576;
        static readonly float[] mean = { 0.485f, 0.456f, 0.406f };
        static readonly float[] std = { 0.229f, 0.224f, 0.225f };
        static readonly float rescaleFactor = 0.00392156862745098f;

        static readonly string[] cocoDatasetClasses = new string[]
        {
            "", "person", "bicycle"
        };

        ~PersonFinder()
        {
            Dispose(false);
        }

        public void CreateSessionAndTensors()
        {
            _session = new InferenceSession(modelPath);
        }

        public List<BoundingBoxResult> LocatePerson(Mat image)
        {
            List<BoundingBoxResult> outputDictionary = new List<BoundingBoxResult>();

            // Load and preprocess image
            using (Mat resized = image.Resize(new Size(targetWidth, targetHeight)))
            using (Mat floatImage = new Mat())
            {
                resized.ConvertTo(floatImage, MatType.CV_32FC3, rescaleFactor);

                // Normalize
                var channels = Cv2.Split(floatImage);
                for (int i = 0; i < 3; i++)
                {
                    channels[i] = (channels[i] - mean[i]) / std[i];
                }
                Cv2.Merge(channels, floatImage);

                // Convert to tensor
                var inputTensor = new DenseTensor<float>(new[] { 1, 3, targetHeight, targetWidth });
                for (int c = 0; c < 3; c++)
                {
                    for (int y = 0; y < targetHeight; y++)
                    {
                        for (int x = 0; x < targetWidth; x++)
                        {
                            inputTensor[0, c, y, x] = floatImage.At<Vec3f>(y, x)[c];
                        }
                    }
                }

                var inputs = new List<NamedOnnxValue>
                    {
                        NamedOnnxValue.CreateFromTensor("pixel_values", inputTensor)
                    };

                using (IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results = _session.Run(inputs))
                {
                    var output = results.First().AsEnumerable<float>().ToArray();

                    // Extract outputs
                    var predBoxes = results.First(r => r.Name == "pred_boxes").AsTensor<float>();
                    var logits = results.First(r => r.Name == "logits").AsTensor<float>();

                    int numBoxes = predBoxes.Dimensions[1]; // 300
                    int numClasses = logits.Dimensions[2];  // 91

                    for (int i = 0; i < numBoxes; i++)
                    {
                        // Get class scores and find the best one
                        float maxScore = float.MinValue;
                        int classId = -1;
                        for (int j = 0; j < numClasses; j++)
                        {
                            float score = logits[0, i, j];
                            if (score > maxScore)
                            {
                                maxScore = score;
                                classId = j;
                            }
                        }

                        // Filter for person (class 0) and bicycle (class 1)
                        if (maxScore > 0.5f && (classId == 0 || classId == 1))
                        {
                            float cx = predBoxes[0, i, 0] * image.Width;
                            float cy = predBoxes[0, i, 1] * image.Height;
                            float w = predBoxes[0, i, 2] * image.Width;
                            float h = predBoxes[0, i, 3] * image.Height;

                            int x1 = (int)(cx - w / 2);
                            int y1 = (int)(cy - h / 2);
                            int x2 = (int)(cx + w / 2);
                            int y2 = (int)(cy + h / 2);

                            if (classId < cocoDatasetClasses.Length)
                            {
                                string label = cocoDatasetClasses[classId];
                                Cv2.Rectangle(image, new Point(x1, y1), new Point(x2, y2), Scalar.Green, 2);
                                outputDictionary.Add(new BoundingBoxResult() { BoundingBox = new Rect2f(x1, y1, x2 - x1, y2 - y1), Label = label, Score = maxScore });
                                //Cv2.PutText(image, $"{label} {maxScore:F2}", new Point(x1, y1 - 5),
                                //        HersheyFonts.HersheySimplex, 0.5, Scalar.Yellow, 1);
                            }
                        }
                    }
                }

                for (int channgelNumber = 0; channgelNumber < channels.Length; channgelNumber++)
                {
                    channels[channgelNumber]?.Dispose();
                }
            }
            return outputDictionary;
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
