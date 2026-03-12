using FitMyBike.Model_Class.Enums;
using FitMyBike.Model_Class.ExtensionMethods;
using FitMyBike.Model_Class.MTAStructure;
using FitMyBike.Model_Class.MTAStructure.Command.RealCommands;
using FitMyBike.Model_Class.MTAStructure.WorkerThreads;
using FitMyBike.Model_Class.PersonFinder;
using FitMyBike.Model_Class.PoseEstimator;
using FitMyBike.View_Class;
using FitMyBike.ViewModel_Class;
using Microsoft.Win32;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xaml.Schema;

namespace FitMyBike
{
    /// <summary>
    /// Logika interakcji dla klasy MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        private ViewModel_Class.ViewModel ViewModel { get; set; }

        internal LineSketcher LineSketcher { get; private set; }
        public bool DragMode { get; private set; }

        public MainWindow()
        {
            InitializeComponent();

            ViewModel = new ViewModel_Class.ViewModel();
            ViewModel.LastKeyFrameHasArrivedEventHandler = KeyframeDictionaryClass_OnLastKeyFrameHasArrived;
            ViewModel.NewKeyFrameHasBeenActivated = new NewKeyFrameHasBeenActivated(HandleActivationOfNewKeyFrame);

            LineSketcher = new LineSketcher();
            foreach (var line in LineSketcher.GetListOfLines())
            {
                MainCanvas.Children.Add(line);
            }

            SetBindings();
        }

        public void SetBindings()
        {
            Binding kneeAngleBinding = new Binding("KneeAngle") { Source = ViewModel.PoseAngleHandlerClass, Mode = BindingMode.TwoWay };
            Binding backAngleBinding = new Binding("BackAngle") { Source = ViewModel.PoseAngleHandlerClass, Mode = BindingMode.OneWay };
            Binding armAngleBinding = new Binding("ArmAngle") { Source = ViewModel.PoseAngleHandlerClass, Mode = BindingMode.OneWay };
            Binding elbowAngleBinding = new Binding("ElbowAngle") { Source = ViewModel.PoseAngleHandlerClass, Mode = BindingMode.OneWay };
            Binding wholeArmAngleBinding = new Binding("WholeArmAngle") { Source = ViewModel.PoseAngleHandlerClass, Mode = BindingMode.OneWay };
            Binding footAngleBinding = new Binding("FootAngle") { Source = ViewModel.PoseAngleHandlerClass, Mode = BindingMode.OneWay };

            KneeAngleLabel.SetBinding(ResultLabel.AngleProperty, kneeAngleBinding);
            BackAngleLabel.SetBinding(ResultLabel.AngleProperty, backAngleBinding);
            ArmAngleLabel.SetBinding(ResultLabel.AngleProperty, armAngleBinding);
            ElbowAngleLabel.SetBinding(ResultLabel.AngleProperty, elbowAngleBinding);
            WholeArmAngleLabel.SetBinding(ResultLabel.AngleProperty, wholeArmAngleBinding);
            FootAngleLabel.SetBinding(ResultLabel.AngleProperty, footAngleBinding);
        }

        private void KeyframeDictionaryClass_OnLastKeyFrameHasArrived(object sender, KeyFrameEventArgs e)
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                e.CyclePhaseDrawer?.DrawLines(CycleValueGraphCanvas);
            }));

            FrameSlider.Dispatcher.Invoke(new Action(() =>
            {
                FrameSlider.Maximum = e.KeyFrameNumber;
            }));
        }

        private void LoadImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = false;
            openFileDialog.Title = "Select image file";
            openFileDialog.Filter = "Image Files (*.jpg *.png *.bmp)|*.jpg;*.png;*.bmp";

            if (openFileDialog.ShowDialog() == true)
            {
                ViewModel.LoadSingleImage(openFileDialog.FileName);
            }
        }

        private void FrameSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            int selectedFrameIndex = (int)Math.Floor(e.NewValue);
            ViewModel.GetSingleFrameResult(selectedFrameIndex);
        }

        private void HandleActivationOfNewKeyFrame(KeyFrameResult currentlyActivatedKeyFrameResult)
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                if (currentlyActivatedKeyFrameResult != null)
                {
                    var widthScale = MainCanvas.ActualWidth / (double)currentlyActivatedKeyFrameResult.Bitmap.Width;
                    var heightScale = MainCanvas.ActualWidth / (double)currentlyActivatedKeyFrameResult.Bitmap.Width;

                    LineSketcher.WidthScale = widthScale;
                    LineSketcher.HeightScale = heightScale;
                    LineSketcher.KeypointResultDict = currentlyActivatedKeyFrameResult.DrawKeypointDictionary;

                    MainImageBox.Source = currentlyActivatedKeyFrameResult.BitmapSource;
                }
            }));
        }


        private void LoadVideo_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = false;
            openFileDialog.Title = "Select video file";
            openFileDialog.Filter = "Video Files (*.mov *.mp4)|*.mov;*.mp4";

            if (openFileDialog.ShowDialog() == true)
            {
                ViewModel.LoadVideo(openFileDialog.FileName);
            }
        }

        double scale = 1.0;
        double minScale = 0.3;
        double maxScale = 3.0;

        System.Windows.Point startPosition;
        bool _moveMode = false;

        PoseKeypoint? ClosestPoseKeypoint {  get; set; }

        ScaleTransform ScaleTransformForMainCanvas = new ScaleTransform();
        TranslateTransform TranslateTransformForMainCanvas = new TranslateTransform();

        public TransformGroup GetTransformGroupForMainCanvas()
        {
            return new TransformGroup() { Children = new TransformCollection(new List<Transform>() { ScaleTransformForMainCanvas, TranslateTransformForMainCanvas }) };
        }

        private void MainCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (MainImageBox.Source != null)
            {
                var position = e.MouseDevice.GetPosition(MainCanvas);

                if (e.Delta > 0)
                    scale += 0.1;
                else
                    scale -= 0.1;

                if (scale > maxScale)
                    scale = maxScale;
                if (scale < minScale)
                    scale = minScale;

                if (ScaleTransformForMainCanvas.ScaleX != scale || ScaleTransformForMainCanvas.ScaleY != scale)
                {
                    ScaleTransformForMainCanvas.CenterX = position.X;
                    ScaleTransformForMainCanvas.CenterY = position.Y;
                    ScaleTransformForMainCanvas.ScaleX = scale;
                    ScaleTransformForMainCanvas.ScaleY = scale;

                    MainCanvas.RenderTransform = GetTransformGroupForMainCanvas();
                }
            }
        }

        private void MainCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!DragMode)
            {
                var canvas = sender as Canvas;
                if (canvas != null)
                {
                    _moveMode = true;
                    startPosition = e.GetPosition(null);
                    canvas.CaptureMouse();
                }
            }
            else
            {
                ClosestPoseKeypoint = LineSketcher.LocateClosestPoint(e.GetPosition(MainCanvas), GetTransformGroupForMainCanvas());
            }
        }

        private void MainCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (!DragMode)
            {
                var canvas = sender as Canvas;
                if (canvas != null && _moveMode)
                {
                    System.Windows.Point currentPosition = e.GetPosition(null);
                    double offsetX = (currentPosition.X - startPosition.X) * 1.0;
                    double offsetY = (currentPosition.Y - startPosition.Y) * 1.0;

                    TranslateTransformForMainCanvas.X += offsetX;
                    TranslateTransformForMainCanvas.Y += offsetY;
                    MainCanvas.RenderTransform = GetTransformGroupForMainCanvas();

                    startPosition = currentPosition;
                }
            }
            else
            {
                LineSketcher.UpdateLineConnectedToSelectedKeypoint(ClosestPoseKeypoint, e.GetPosition(MainCanvas));
                ViewModel.UpdateKeyFrameAfterUserInput();
            }
        }

        private void MainCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!DragMode)
            {
                var canvas = sender as Canvas;
                if (canvas != null)
                {
                    _moveMode = false;
                    canvas.ReleaseMouseCapture();
                }
            }
            else
            {
                ClosestPoseKeypoint = null;
            }
        }

        private void MainCanvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var canvas = sender as Canvas;
            if (canvas != null)
            {
                scale = 1.0;
                ScaleTransformForMainCanvas = new ScaleTransform();
                TranslateTransformForMainCanvas = new TranslateTransform();
                MainCanvas.RenderTransform = GetTransformGroupForMainCanvas();
            }
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch(e.Key)
            {
                case Key.LeftCtrl:
                    DragMode = true;
                    break;
            }
        }

        private void Window_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.LeftCtrl:
                    DragMode = false;
                    ClosestPoseKeypoint = null;
                    break;
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.StopOperations();
            this.Close();
        }
    }
}
