using Microsoft.Kinect;
using Smithers.Sessions.Archiving;
using Smithers.Visualization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Animation;

using Smithers.Reading.FrameData.Mock;
using Smithers.Sessions;

namespace Monocle
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ApplicationController _appController;
        CaptureController _captureController;
        CameraImagePresenter _cameraImagePresenter;
        private ProjectionMode _projectionMode;
        private Storyboard _flashAttack;
        private Storyboard _flashDecay;
        private KinectSensor _sensor;

        private object _lockObject = new object();

        private System.Timers.Timer _fakeKinectDataTimer;
        private MockLiveFrame _fakeLiveFrame = MockLiveFrame.GetFakeLiveFrame();
        private int _framesToCapture;

        public MainWindow()
        {
            InitializeComponent();

            _appController = new ApplicationController();
            _captureController = _appController.CaptureController;

            _sensor = KinectSensor.GetDefault();
            _sensor.Open();


            _cameraImagePresenter = new CameraImagePresenter(camera, cameraDummpy);
            _cameraImagePresenter.CameraMode = CameraMode.Color;
            _cameraImagePresenter.Enabled = true;
            //camera.Source = _captureController.ColorBitmap.Bitmap;

            _captureController.SessionManager.ShotBeginning += (sender, e) =>
            {
                _flashAttack.Begin();
                _cameraImagePresenter.Enabled = false;
            };

            _captureController.SessionManager.ShotCompletedSuccess += (sender, e) =>
            {
                _cameraImagePresenter.Enabled = true;
            };

            _captureController.SessionManager.ShotCompletedError += (sender, e) =>
            {
                _flashDecay.Begin();
                MessageBox.Show(e.ErrorMessage);
                _cameraImagePresenter.Enabled = true;
            };

            _captureController.SessionManager.ShotSavedSuccess += (sender, e) =>
            {
                lock (_lockObject)
                {
                // TODO: check if this works on the real kinect, it crashes with the fake setup
                    if (checkBox.IsChecked != true)
                    {
                        _flashDecay.Begin();
                        lblCaptureCount.Content = _captureController.Session.Shots.Where(x => x.Completed).Count();
                    }
                }
            };

            _captureController.SessionManager.ShotSavedError += (sender, e) =>
            {
                _flashDecay.Begin();
                if (e.Exception == null)
                    MessageBox.Show(e.ErrorMessage);
                else
                    MessageBox.Show(e.ErrorMessage + ": " + e.Exception.Message);
            };

            _captureController.SkeletonPresenter = new SkeletonPresenter(canvas);
            _captureController.SkeletonPresenter.ShowBody = true;
            _captureController.SkeletonPresenter.ShowHands = true;
            _captureController.FrameReader.AddResponder(_captureController.SkeletonPresenter);
            _captureController.SkeletonPresenter.ProjectionMode = ProjectionMode.COLOR_IMAGE;

            _captureController.SkeletonPresenter.CoordinateMapper = KinectSensor.GetDefault().CoordinateMapper;
            _captureController.SkeletonPresenter.Underlay = camera;

            _captureController.FrameReader.AddResponder(_cameraImagePresenter);

            _flashAttack = FindResource("FlashAttack") as Storyboard;
            _flashDecay = FindResource("FlashDecay") as Storyboard;



            // Timer that fires every 33ms (~30 fps). When the timer fires, the SessionManager receives
            // a FrameArrived Event just like he would from the real Kinect.
            // The timer is only enabled if the "Send Fake Kinect Data" Checkbox is toggled.
            _fakeKinectDataTimer = new System.Timers.Timer(33);
            _fakeKinectDataTimer.Elapsed += new System.Timers.ElapsedEventHandler(onTimerElapsed);

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (checkBox.IsChecked == true)
            {
                if (!_fakeKinectDataTimer.Enabled)
                {
                    _fakeKinectDataTimer.Enabled = true;
                }
            }
            
            try
            {
                int nFramesToCapture = Convert.ToInt32(nFramesToCaptureText.Text);
                _framesToCapture = nFramesToCapture;
                int nMemoryFrames = Convert.ToInt32(nMemoryFramesText.Text); 

                SerializationFlags serializationFlags =
                    new SerializationFlags(ColorBox.IsChecked == true, 
                                           DepthBox.IsChecked == true,
                                           InfraredBox.IsChecked == true,
                                           SkeletonBox.IsChecked == true,
                                           DepthMappingBox.IsChecked == true);
                _captureController.StartCapture(serializationFlags, nMemoryFrames, nFramesToCapture);
            }
            catch (Exception ex)
            {
                // Catch those Exceptions we know can happen
                if (ex is FormatException || ex is ArgumentException || ex is ArgumentOutOfRangeException ||
                    ex is OverflowException || ex is InvalidOperationException)
                {
                    Console.WriteLine("Exception thrown");
                    MessageBox.Show(ex.Message);
                    return;
                }

                // Other type of Exception, so rethrow it
                throw;
            }
             
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            _captureController.StopCapture();
        }

        private void onTimerElapsed(object source, System.Timers.ElapsedEventArgs e)
        {
           //  Console.WriteLine("The Elapsed event was raised at {0}", e.SignalTime);
            this._captureController.SessionManager.FrameArrived(_fakeLiveFrame);
        }

        private void ToggleCamera_Click(object sender, RoutedEventArgs e)
        {
            string modeString = (sender as ToggleButton).Tag.ToString();
            CameraMode cameraMode = (CameraMode)Enum.Parse(typeof(CameraMode), modeString);
            
            foreach (var child in spCamera.Children)
            {
                if (child is ToggleButton)
                {
                    (child as ToggleButton).IsChecked = child == sender;
                }
            }
            if (cameraMode == CameraMode.Color || cameraMode == CameraMode.ColorDepth)
                _projectionMode = ProjectionMode.COLOR_IMAGE;
            else
                _projectionMode = ProjectionMode.DEPTH_IMAGE;
            _captureController.SkeletonPresenter.ProjectionMode = _projectionMode;

            _cameraImagePresenter.CameraMode = cameraMode;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            KinectSensor.GetDefault().Close();
            _captureController.FrameReader.Dispose();

            Environment.Exit(0);
        }

        private async void Compress_Click(object sender, RoutedEventArgs e)
        {
            lblCaptureCount.Content = "Compressing...";
            ArchiveResult result = await _appController.CompressAndStartNewSession();
            if (result.Success)
                lblCaptureCount.Content = "Compressed successfully";
            else
                lblCaptureCount.Content = string.Format("Archive failed: {0}", result.Exception.Message);
        }

    }
}
