using FitMyBike.Model_Class.Enums;
using FitMyBike.View_Class;
using Newtonsoft.Json.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Management;

namespace FitMyBike.Model_Class.MTAStructure
{
    public delegate void KeyframeDictionaryClass_LastKeyFrameHasArrived(object sender, KeyFrameEventArgs e);

    internal class KeyframeDictionaryClass
    {
        private Dictionary<int, KeyFrameResult> keyFrameResultDictionary = new Dictionary<int, KeyFrameResult>();
        private readonly object _lock = new object();

        private int _frameQty = 0;

        public event KeyframeDictionaryClass_LastKeyFrameHasArrived OnLastKeyFrameHasArrived;

        public void Invoke_OnLastKeyFrameHasArrived(int indexOfLastFrame, bool calculateResults = false)
        {
            CyclePhaseDrawer cyclePhaseDrawer = null;
            if (indexOfLastFrame > 1 && indexOfLastFrame == _frameQty)
            {
                PoseAngleHandlerClass.CalculatePeaksAndLows(out Dictionary<PoseAngles, IEnumerable<double>> angleMaximasDict, out Dictionary<PoseAngles, IEnumerable<double>> angleMinimaDict);
                cyclePhaseDrawer = new CyclePhaseDrawer(angleMinimaDict[PoseAngles.KneeAngle], angleMaximasDict[PoseAngles.KneeAngle], PoseAngleHandlerClass.PoseAngleHandlerHistorianDict.Values.Select(x => x[Enums.PoseAngles.KneeAngle]));
            }
            OnLastKeyFrameHasArrived?.Invoke(this, new KeyFrameEventArgs() { KeyFrameNumber = indexOfLastFrame, KeyFrameResult = keyFrameResultDictionary[indexOfLastFrame], CyclePhaseDrawer = cyclePhaseDrawer });
        }

        public KeyFrameResult GetSingleFrameResult(int frameIndex)
        {
            lock (_lock)
            {
                if (keyFrameResultDictionary.ContainsKey(frameIndex))
                {
                    return keyFrameResultDictionary[frameIndex];
                }
                else
                {
                    return null;
                }
            }
        }

        public void SetNumberOfFrames(int frameNumber)
        {
            _frameQty = frameNumber;
        }

        public void InsertSingleFrameResult(int frameIndex, KeyFrameResult keyFrameResult)
        {
            lock (_lock)
            {
                if (keyFrameResultDictionary.ContainsKey(frameIndex))
                {
                    keyFrameResultDictionary[frameIndex] = keyFrameResult;
                }
                else
                {
                    keyFrameResultDictionary.Add(frameIndex, keyFrameResult);
                }

                Invoke_OnLastKeyFrameHasArrived(frameIndex);

                if (keyFrameResult.IsLast == true)
                {
                    Invoke_OnLastKeyFrameHasArrived(frameIndex, calculateResults: true);
                }
            }
        }

        public void Reset()
        {
            lock (_lock)
            {
                keyFrameResultDictionary.Clear();
            }
        }
    }

}
