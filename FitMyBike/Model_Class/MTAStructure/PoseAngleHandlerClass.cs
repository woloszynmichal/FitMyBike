using FitMyBike.Model_Class.Enums;
using FitMyBike.Model_Class.PoseEstimator;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitMyBike.Model_Class.MTAStructure
{
    internal class PoseAngleHandlerClass : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private static Dictionary<int, Dictionary<PoseAngles, double>> _PoseAngleHandlerHistorianDict = new Dictionary<int, Dictionary<PoseAngles, double>>();

        private Dictionary<PoseAngles, double> _poseAngleDict = new Dictionary<PoseAngles, double>()
        {
            { PoseAngles.KneeAngle, 0},
            { PoseAngles.BackAngle, 0},
            { PoseAngles.ArmAngle, 0},
            { PoseAngles.ElbowAngle, 0},
            { PoseAngles.WholeArmAngle, 0},
            { PoseAngles.FootAngle, 0},
        };

        private Dictionary<PoseAngles, double> PoseAngleDict 
        {
            get => _poseAngleDict;
        }

        public static Dictionary<int, Dictionary<PoseAngles, double>> PoseAngleHandlerHistorianDict { get => _PoseAngleHandlerHistorianDict; }

        public double KneeAngle
        {
            get => PoseAngleDict[PoseAngles.KneeAngle];
            set
            {
                PoseAngleDict[PoseAngles.KneeAngle] = value;
                OnPropertyChanged(nameof(KneeAngle));
            }
        }

        public double BackAngle
        {
            get => PoseAngleDict[PoseAngles.BackAngle];
            set
            {
                PoseAngleDict[PoseAngles.BackAngle] = value;
                OnPropertyChanged(nameof(BackAngle));
            }
        }

        public double ArmAngle
        {
            get => PoseAngleDict[PoseAngles.ArmAngle];
            set
            {
                PoseAngleDict[PoseAngles.ArmAngle] = value;
                OnPropertyChanged(nameof(ArmAngle));
            }
        }

        public double ElbowAngle
        {
            get => PoseAngleDict[PoseAngles.ElbowAngle];
            set
            {
                PoseAngleDict[PoseAngles.ElbowAngle] = value;
                OnPropertyChanged(nameof(ElbowAngle));
            }
        }

        public double WholeArmAngle
        {
            get => PoseAngleDict[PoseAngles.WholeArmAngle];
            set
            {
                PoseAngleDict[PoseAngles.WholeArmAngle] = value;
                OnPropertyChanged(nameof(WholeArmAngle));
            }
        }

        public double FootAngle
        {
            get => PoseAngleDict[PoseAngles.FootAngle];
            set
            {
                PoseAngleDict[PoseAngles.FootAngle] = value;
                OnPropertyChanged(nameof(FootAngle));
            }
        }

        public void GetTheAngles(Dictionary<PoseKeypoint, KeypointResult> keypoints, int keyframeNumber)
        {
            var earPoint = keypoints.ContainsKey(PoseKeypoint.Ear) ? keypoints[PoseKeypoint.Ear] : null;
            var shoulderPoint = keypoints.ContainsKey(PoseKeypoint.Shoulder) ? keypoints[PoseKeypoint.Shoulder] : null;
            var elbowPoint = keypoints.ContainsKey(PoseKeypoint.Elbow) ? keypoints[PoseKeypoint.Elbow] : null;
            var wristPoint = keypoints.ContainsKey(PoseKeypoint.Wrist) ? keypoints[PoseKeypoint.Wrist] : null;
            var hipPoint = keypoints.ContainsKey(PoseKeypoint.Hip) ? keypoints[PoseKeypoint.Hip]     : null;
            var kneePoint = keypoints.ContainsKey(PoseKeypoint.Knee) ? keypoints[PoseKeypoint.Knee] : null;
            var anklePoint = keypoints.ContainsKey(PoseKeypoint.Ankle) ? keypoints[PoseKeypoint.Ankle] : null;
            var heelPoint = keypoints.ContainsKey(PoseKeypoint.Heel) ? keypoints[PoseKeypoint.Heel]    : null;
            var toesPoint = keypoints.ContainsKey(PoseKeypoint.Toes) ? keypoints[PoseKeypoint.Toes] : null;

            var vectorHipKnee = hipPoint?.CalculateVectorToAnotherKeypointResult(kneePoint);
            var vectorAnkleKnee = anklePoint?.CalculateVectorToAnotherKeypointResult(kneePoint);
            var vectorKneeHip = kneePoint?.CalculateVectorToAnotherKeypointResult(hipPoint);
            var vectorShoulderHip = shoulderPoint?.CalculateVectorToAnotherKeypointResult(hipPoint);
            var vectorHipShoulder = hipPoint?.CalculateVectorToAnotherKeypointResult(shoulderPoint);
            var vectorElbowShoulder = elbowPoint?.CalculateVectorToAnotherKeypointResult(shoulderPoint);
            var vectorWristElbow = wristPoint?.CalculateVectorToAnotherKeypointResult(elbowPoint);
            var vectorShoulderElbow = shoulderPoint?.CalculateVectorToAnotherKeypointResult(elbowPoint);
            var vectorWristShoulder = wristPoint?.CalculateVectorToAnotherKeypointResult(shoulderPoint);
            var vectorToesHeel = toesPoint?.CalculateVectorToAnotherKeypointResult(heelPoint);
            var vectorKneeAnkle = kneePoint?.CalculateVectorToAnotherKeypointResult(anklePoint);

            KneeAngle = GetAngleFromVectors(vectorHipKnee, vectorAnkleKnee);
            BackAngle = GetAngleFromVectors(vectorShoulderHip, vectorKneeHip);
            ArmAngle = GetAngleFromVectors(vectorHipShoulder, vectorElbowShoulder);
            ElbowAngle = GetAngleFromVectors(vectorWristElbow, vectorShoulderElbow);
            WholeArmAngle = GetAngleFromVectors(vectorHipShoulder, vectorWristShoulder);
            FootAngle = GetAngleFromVectors(vectorToesHeel, vectorKneeAnkle);

            if(_PoseAngleHandlerHistorianDict.ContainsKey(keyframeNumber))
            {
                _PoseAngleHandlerHistorianDict[keyframeNumber] = new Dictionary<PoseAngles, double>(PoseAngleDict);
            }
            else
            {
                _PoseAngleHandlerHistorianDict.Add(keyframeNumber, new Dictionary<PoseAngles, double>(PoseAngleDict));
            }
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

        public static void CalculatePeaksAndLows(out Dictionary<PoseAngles, IEnumerable<double>> angleMaximasDict, out Dictionary<PoseAngles, IEnumerable<double>> angleMinimasDict)
        {
            var kneeAngleLocalMaxima = LocalMaxima(_PoseAngleHandlerHistorianDict.Values.Select(x => x[Enums.PoseAngles.KneeAngle]), 4).Select(x => x.Item2);
            var armAngleLocalMaxima = LocalMaxima(_PoseAngleHandlerHistorianDict.Values.Select(x => x[Enums.PoseAngles.ArmAngle]), 4).Select(x => x.Item2);
            var elbowAngleLocalMaxima = LocalMaxima(_PoseAngleHandlerHistorianDict.Values.Select(x => x[Enums.PoseAngles.ElbowAngle]), 4).Select(x => x.Item2);
            var backAngleLocalMaxima = LocalMaxima(_PoseAngleHandlerHistorianDict.Values.Select(x => x[Enums.PoseAngles.BackAngle]), 4).Select(x => x.Item2);
            var wholeArmAngleLocalMaxima = LocalMaxima(_PoseAngleHandlerHistorianDict.Values.Select(x => x[Enums.PoseAngles.WholeArmAngle]), 4).Select(x => x.Item2);
            var footAngleLocalMaxima = LocalMaxima(_PoseAngleHandlerHistorianDict.Values.Select(x => x[Enums.PoseAngles.FootAngle]), 4).Select(x => x.Item2);

            angleMaximasDict = new Dictionary<PoseAngles, IEnumerable<double>>()
            {
                { PoseAngles.KneeAngle, kneeAngleLocalMaxima },
                { PoseAngles.BackAngle, backAngleLocalMaxima },
                { PoseAngles.ArmAngle, armAngleLocalMaxima },
                { PoseAngles.ElbowAngle, elbowAngleLocalMaxima },
                { PoseAngles.WholeArmAngle, wholeArmAngleLocalMaxima },
                { PoseAngles.FootAngle, footAngleLocalMaxima },
            };

            var kneeAngleLocalMinima = LocalMinima(_PoseAngleHandlerHistorianDict.Values.Select(x => x[Enums.PoseAngles.KneeAngle]), 4).Select(x => x.Item2);
            var armAngleLocalMinima = LocalMinima(_PoseAngleHandlerHistorianDict.Values.Select(x => x[Enums.PoseAngles.ArmAngle]), 4).Select(x => x.Item2);
            var elbowAngleLocalMinima = LocalMinima(_PoseAngleHandlerHistorianDict.Values.Select(x => x[Enums.PoseAngles.ElbowAngle]), 4).Select(x => x.Item2);
            var backAngleLocalMinima = LocalMinima(_PoseAngleHandlerHistorianDict.Values.Select(x => x[Enums.PoseAngles.BackAngle]), 4).Select(x => x.Item2);
            var wholeArmAngleLocalMinima = LocalMinima(_PoseAngleHandlerHistorianDict.Values.Select(x => x[Enums.PoseAngles.WholeArmAngle]), 4).Select(x => x.Item2);
            var footAngleLocalMinima = LocalMinima(_PoseAngleHandlerHistorianDict.Values.Select(x => x[Enums.PoseAngles.FootAngle]), 4).Select(x => x.Item2);

            angleMinimasDict = new Dictionary<PoseAngles, IEnumerable<double>>()
            {
                { PoseAngles.KneeAngle, kneeAngleLocalMinima },
                { PoseAngles.BackAngle, backAngleLocalMinima },
                { PoseAngles.ArmAngle, armAngleLocalMinima },
                { PoseAngles.ElbowAngle, elbowAngleLocalMinima },
                { PoseAngles.WholeArmAngle, wholeArmAngleLocalMinima },
                { PoseAngles.FootAngle, footAngleLocalMinima },
            };
        }

        public static IEnumerable<Tuple<int, double>> LocalMaxima(IEnumerable<double> source, int windowSize)
        {
            // Round up to nearest odd value
            windowSize = windowSize - windowSize % 2 + 1;
            int halfWindow = windowSize / 2;

            int index = 0;
            var before = new Queue<double>(Enumerable.Repeat(double.NegativeInfinity, halfWindow));
            var after = new Queue<double>(source.Take(halfWindow + 1));

            foreach (double d in source.Skip(halfWindow + 1).Concat(Enumerable.Repeat(double.NegativeInfinity, halfWindow + 1)))
            {
                double curVal = after.Dequeue();
                if (before.All(x => curVal > x) && after.All(x => curVal >= x))
                {
                    yield return Tuple.Create(index, curVal);
                }
                before.Enqueue(curVal);
                before.Dequeue();
                after.Enqueue(d);
                index++;
            }
        }

        public static IEnumerable<Tuple<int, double>> LocalMinima(IEnumerable<double> source, int windowSize)
        {
            // Round up to nearest odd value
            windowSize = windowSize - windowSize % 2 + 1;
            int halfWindow = windowSize / 2;

            int index = 0;
            var before = new Queue<double>(Enumerable.Repeat(double.NegativeInfinity, halfWindow));
            var after = new Queue<double>(source.Take(halfWindow + 1));

            foreach (double d in source.Skip(halfWindow + 1).Concat(Enumerable.Repeat(double.NegativeInfinity, halfWindow + 1)))
            {
                double curVal = after.Dequeue();
                if (before.All(x => curVal <= x) && after.All(x => curVal < x))
                {
                    yield return Tuple.Create(index, curVal);
                }
                before.Enqueue(curVal);
                before.Dequeue();
                after.Enqueue(d);
                index++;
            }
        }

        public void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        internal void Reset()
        {
            KneeAngle = 0;
            BackAngle = 0;
            ArmAngle = 0;
            ElbowAngle = 0;
            WholeArmAngle = 0;
            FootAngle = 0;

            PoseAngleHandlerHistorianDict.Clear();
        }
    }
}
