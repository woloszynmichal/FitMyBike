using FitMyBike.Model_Class.Enums;
using FitMyBike.Model_Class.MTAStructure;
using FitMyBike.Model_Class.MTAStructure.Command.RealCommands;
using FitMyBike.Model_Class.MTAStructure.WorkerThreads;
using FitMyBike.Model_Class.PersonFinder;
using FitMyBike.Model_Class.PoseEstimator;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

internal delegate void NewKeyFrameHasBeenActivated(KeyFrameResult currentlyActivatedKeyFrameResult);

namespace FitMyBike.ViewModel_Class
{
    internal class ViewModel
    {
        private KeyFrameResult _currentlyActivatedKeyFrameResult = null;
        private int _currentFrameIndex = 0;

        private PersonFinder PersonFinder { get; set; }
        private PoseEstimator PoseEstimator { get; set; }
        private KeyframeDictionaryClass KeyframeDictionaryClass { get; set; }

        private WorkerThread<EstimatePersonLocationCommand> PersonLocator_WorkerThread { get; set; }
        private WorkerThread<EstimatePersonPoseCommand> PoseEstimator_WorkerThread { get; set; }

        private KeyFrameResult CurrentlyActivatedKeyFrameResult
        {
            get => _currentlyActivatedKeyFrameResult;
            set
            {
                _currentlyActivatedKeyFrameResult = value;
                NewKeyFrameHasBeenActivated.Invoke(_currentlyActivatedKeyFrameResult);
            }
        }

        internal PoseAngleHandlerClass PoseAngleHandlerClass { get; private set; }



        public NewKeyFrameHasBeenActivated NewKeyFrameHasBeenActivated { get; set; }

        public KeyframeDictionaryClass_LastKeyFrameHasArrived LastKeyFrameHasArrivedEventHandler
        {
            set
            {
                KeyframeDictionaryClass.OnLastKeyFrameHasArrived += value;
            }
        }

        public ViewModel()
        {
            PersonLocator_WorkerThread = new WorkerThread<EstimatePersonLocationCommand>("PersonLocator_WorkerThread");
            PoseEstimator_WorkerThread = new WorkerThread<EstimatePersonPoseCommand>("PoseEstimator_WorkerThread");

            KeyframeDictionaryClass = new Model_Class.MTAStructure.KeyframeDictionaryClass();
            KeyframeDictionaryClass.OnLastKeyFrameHasArrived += ViewModel_KeyFrameHasArrived;

            PersonFinder = new PersonFinder();
            PersonFinder.CreateSessionAndTensors();

            PoseEstimator = new PoseEstimator();
            PoseEstimator.CreateSessionAndTensors();

            PoseAngleHandlerClass = new PoseAngleHandlerClass();
        }

        private void ViewModel_KeyFrameHasArrived(object sender, KeyFrameEventArgs e)
        {
            PoseAngleHandlerClass.GetTheAngles(e.KeyFrameResult.DrawKeypointDictionary, e.KeyFrameNumber);
            CurrentlyActivatedKeyFrameResult = e.KeyFrameResult;
            _currentFrameIndex = e.KeyFrameNumber;
        }

        private void GenerateCommand(Mat image, int frameIndex, bool isLastFrame = false)
        {
            EstimatePersonLocationCommand estimatePersonLocationCommand = new EstimatePersonLocationCommand()
            {
                ImageFrame = image,
                PoseEstimator = this.PoseEstimator,
                PersonFinder = this.PersonFinder,
                KeyframeDictionaryClass = this.KeyframeDictionaryClass,
                KeyFrame = frameIndex,
                IsLast = isLastFrame
            };
            estimatePersonLocationCommand.EnqueueThisCommand();
        }

        public void LoadSingleImage(string filePath)
        {
            Reset();
            KeyframeDictionaryClass.SetNumberOfFrames(0);

            if (File.Exists(filePath))
            {
                Mat image = new Mat(filePath);
                GenerateCommand(image, 0);
            }
        }

        public void LoadVideo(string filePath)
        {
            Reset();

            if (File.Exists(filePath))
            {
                Task.Factory.StartNew(() =>
                {
                    using (var capture = new VideoCapture(filePath))
                    {
                        int frameIndex = -1;

                        while (capture.Grab())
                        {
                            Mat frame = new Mat();
                            capture.Retrieve(frame);

                            if (frame != null)
                            {
                                frameIndex++;
                                GenerateCommand(frame, frameIndex, frameIndex == capture.FrameCount - 1);

                                if (frameIndex == capture.FrameCount - 1) { break; }
                            }

                        }

                        KeyframeDictionaryClass.SetNumberOfFrames(frameIndex);
                    }
                });
            }
        }

        private void Reset()
        {
            KeyframeDictionaryClass.Reset();
            PoseAngleHandlerClass.Reset();

            CurrentlyActivatedKeyFrameResult = null;
            _currentFrameIndex = 0;
        }

        internal void GetSingleFrameResult(int selectedFrameIndex)
        {
            CurrentlyActivatedKeyFrameResult = KeyframeDictionaryClass.GetSingleFrameResult(selectedFrameIndex);
            if (CurrentlyActivatedKeyFrameResult != null)
            {
                PoseAngleHandlerClass.GetTheAngles(_currentlyActivatedKeyFrameResult.DrawKeypointDictionary, selectedFrameIndex);
                _currentFrameIndex = selectedFrameIndex;
            }
        }

        internal void UpdateKeyFrameAfterUserInput()
        {
            PoseAngleHandlerClass.GetTheAngles(_currentlyActivatedKeyFrameResult.DrawKeypointDictionary, _currentFrameIndex);
        }

        internal void StopOperations()
        {
            PersonLocator_WorkerThread.KillThisWorkerThread();
            PoseEstimator_WorkerThread.KillThisWorkerThread();
            PersonFinder.Dispose();
            PoseEstimator.Dispose();
        }
    }
}
