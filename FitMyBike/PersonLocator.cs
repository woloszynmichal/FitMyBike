using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.ML.OnnxRuntime;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitMyBike
{
    internal class PersonLocator
    {
        static readonly int targetWidth = 576;
        static readonly int targetHeight = 576;
        static readonly float[] mean = { 0.485f, 0.456f, 0.406f };
        static readonly float[] std = { 0.229f, 0.224f, 0.225f };
        static readonly float rescaleFactor = 0.00392156862745098f; // 1/255

        static readonly string[] cocoClasses = new string[]
        {
            "", "person", "bicycle"
        };

        public static Dictionary<string, Rect2f> LocatePerson(Mat image)
        {
            Dictionary<string, Rect2f> outputDictionary = new Dictionary<string, Rect2f>();
            string modelPath = @"DL_Models\person_locator.onnx";

            // Load and preprocess image
            Mat resized = image.Resize(new Size(targetWidth, targetHeight));
            Mat floatImage = new Mat();
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

            // Run inference
            using (var session = new InferenceSession(modelPath))
            {
                var inputs = new List<NamedOnnxValue>
                {
                    NamedOnnxValue.CreateFromTensor("pixel_values", inputTensor)
                };

                using (IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results = session.Run(inputs))
                {
                    var output = results.First().AsEnumerable<float>().ToArray();

                    // Postprocess (example assumes output format: [num_detections, 6] => [x1, y1, x2, y2, score, class_id])
                    //int numDetections = output.Length / 6;
                    //for (int i = 0; i < numDetections; i++)
                    //{
                    //    float score = output[i * 6 + 4];
                    //    int classId = (int)output[i * 6 + 5];

                    //    if (score > 0.5f && (cocoClasses[classId] == "person" || cocoClasses[classId] == "bicycle"))
                    //    {
                    //        int x1 = (int)(output[i * 6 + 0] * image.Width / targetWidth);
                    //        int y1 = (int)(output[i * 6 + 1] * image.Height / targetHeight);
                    //        int x2 = (int)(output[i * 6 + 2] * image.Width / targetWidth);
                    //        int y2 = (int)(output[i * 6 + 3] * image.Height / targetHeight);

                    //        Cv2.Rectangle(image, new Point(x1, y1), new Point(x2, y2), Scalar.Red, 2);
                    //        Cv2.PutText(image, $"{cocoClasses[classId]} {score:F2}", new Point(x1, y1 - 5),
                    //                    HersheyFonts.HersheySimplex, 0.5, Scalar.Yellow, 1);
                    //    }
                    //}

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

                            if (classId < cocoClasses.Length) {
                                string label = cocoClasses[classId];
                                Cv2.Rectangle(image, new Point(x1, y1), new Point(x2, y2), Scalar.Green, 2);
                                outputDictionary.Add(label, new Rect2f(x1, y1, x2 - x1, y2 - y1));
                                Cv2.PutText(image, $"{label} {maxScore:F2}", new Point(x1, y1 - 5),
                                        HersheyFonts.HersheySimplex, 0.5, Scalar.Yellow, 1);
                            }
                        }
                    }

                    //Cv2.ImShow("Detections", image);
                    //Cv2.WaitKey();
                }
            }

            return outputDictionary;
        }
    }
}
