using H_ImageCapture_System.Struct;
using MahApps.Metro;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using OpenCvSharp;
using CameraManager;
using MahApps.Metro.Controls;
using Microsoft.WindowsAPICodePack.Dialogs;
using Aladdin.HASP;

namespace H_ImageCapture_System
{
    public partial class MainEngine
    {
        public bool isRunning = true;

        private Hansero.LogManager logManager;

        public Thread mainSocketCheckThread;

        public delegate void OnMessageReceiveEventHander(string message, DateTime dateTime, string type);
        public event OnMessageReceiveEventHander OnMessageReceiveEvent = delegate { };

        public bool flagStopImageSaveThread = false;
        private Thread imageSaveThread = null;

        public readonly object lockDisplayObj = new object();

        private HCamera selectedCamera;
        public HCamera SelectedCamera
        {
            get { return selectedCamera; }
            set
            {
                selectedCamera = value;

                /*
                if (selectedCamera != null)
                {
                    IsSelectedCamera = true;
                }
                else
                {
                    IsSelectedCamera = false;
                }
                */

                if (selectedCamera != null)
                {
                    Exposure = Convert.ToInt32(selectedCamera.Parameters.Exposure);
                    ExposureMin = Convert.ToInt32(selectedCamera.Parameters.ExposureMin);
                    ExposureMax = Convert.ToInt32(selectedCamera.Parameters.ExposureMax);
                    ExposureInterval = Convert.ToInt32(selectedCamera.Parameters.ExposureInterval);
                }
            }
        }

        private List<HCamera> selectedCameraList = new List<HCamera>();
        public List<HCamera> SelectedCameraList
        {
            get
            {
                return selectedCameraList;
            }
            set
            {
                selectedCameraList = value;
            }
        }

        public HCameraManager cameraManager;

        private bool isContinousShot = false;

        public string SensorReadoutMode { get { return sensorReadoutMode; } set { } }
        private string sensorReadoutMode = "";

        public double CaptureSpeed
        {
            get
            {
                try
                {
                    double value = Math.Round((1000.0 / FrameRate));

                    if (selectedCameraList.Count > 0)
                    {
                        for (int i = 0; i < selectedCameraList.Count; i++)
                        {
                            selectedCameraList[i].CaptureSpeed = (int)value;
                        }
                    }

                    return value;
                }
                catch (Exception e)
                {
                    logManager.Error(e.Message);
                    return 0;
                }
            }
        }


        public void SetWhiteBalance()
        {
            // m_imageProvider.SetWhiteBalance();
            // selectedCamera.camera.MV_CC_SetEnumValue_NET();
        }

        public string Start_ContinuousShot_Key { get { return configIni.GetString("ShortCut_Key", "Start_ContinuousShot_Key", "A"); } }

        public string Stop_ContinuousShot_Key { get { return configIni.GetString("ShortCut_Key", "Stop_ContinuousShot_Key", "S"); } }

        Queue<string> SendBitmapPathQueue = new Queue<string>();

        public int CaptureCount { get; set; }

        public int DisplayCount { get; set; }

        public int SaveCount { get; set; }


        public int TotalCaptureIntval { get { return totalCaptureIntval; } set { totalCaptureIntval = value; } }
        private int totalCaptureIntval = 0;

        public bool FlagStopContinuousLimitShot = false;

        IniFile configIni;

        #region 테마 관련 
        public class AccentColorMenuData
        {
            IniFile iniFile = new IniFile(AppDomain.CurrentDomain.BaseDirectory + "Config.ini");

            public string Name { get; set; }
            public Brush BorderColorBrush { get; set; }
            public Brush ColorBrush { get; set; }

            private ICommand changeAccentCommand;

            public ICommand ChangeAccentCommand
            {
                get { return this.changeAccentCommand ?? (changeAccentCommand = new SimpleCommand { CanExecuteDelegate = x => true, ExecuteDelegate = x => this.DoChangeTheme(x) }); }
            }

            //엑센트 색상 변경
            public virtual void DoChangeTheme(object sender)
            {
                var theme = ThemeManager.DetectAppStyle(Application.Current);
                var accent = ThemeManager.GetAccent(this.Name);
                ThemeManager.ChangeAppStyle(Application.Current, accent, theme.Item1);

                iniFile.WriteValue("Theme", "Accent", this.Name);
            }
        }

        public class AppThemeMenuData : AccentColorMenuData
        {
            IniFile iniFile = new IniFile(AppDomain.CurrentDomain.BaseDirectory + "Config.ini");
            //테마 색상 변경
            public override void DoChangeTheme(object sender)
            {
                var theme = ThemeManager.DetectAppStyle(Application.Current);
                var appTheme = ThemeManager.GetAppTheme(this.Name);
                ThemeManager.ChangeAppStyle(Application.Current, theme.Item2, appTheme);

                iniFile.WriteValue("Theme", "Theme", this.Name);
            }
        }

        public List<AccentColorMenuData> AccentColors { get; set; }
        public List<AppThemeMenuData> AppThemes { get; set; }

        #endregion

        private Process[] procs = Process.GetProcessesByName("H_Czech_Under_Body_Image_Acquisition_W");

        private MetroWindow window;

        public MainEngine(MetroWindow window)
        {
            logManager = new Hansero.LogManager(true, true);

            // 이미 실행되어 있을 시 종료
            if (procs.Length > 1)
            {
                // Application.Current.Shutdown(0);
                Environment.Exit(0);
            }

            HaspFeature feature = HaspFeature.Default;

            string vendorCode =
            "6fwzIVvXWPBcegZRGldsMOlLxjCMm0OiKhK3yGqWl/r6L9nJw34ycuzhncNj8wSYJBs6n+FP2H3+QWcO" +
            "qfk+o4gzcgIqsIywrphntVkG1Dey6bZSHL3+JhU6vYheKIxMK9uMWiPiRD3tLI79pRM9ZO+FzdHfJSLR" +
            "o10ERQGE0KYT20qrxAH7DXMx5rZxSTRzYo2XBl0YQYc/mwhmyMZWuVPNbF/7kcccATdSyaQ9lUz1/pSw" +
            "fOTwIUny5s0ktim4iiGWtZN+JzOff1rkzWAQVAKbrFQUMXAk7qnvK++0dttpUwvN4zTQoAAkbw93C8IR" +
            "0p6oVVEv+C8mU9XyCeVSOixEhMhxGKyD/v73/9pEZPpe/C7KgEBLwQGTCW4WMjDt7s7vZMWguPsACqTd" +
            "eS0PU1qZ2U7M6NQUsl8OaES+G8zp4Xtpzt/kij+awDpgCCVYMkWN7DsqaAMVFqkUGHGmnChRCnRG4NQU" +
            "OJKDGqhCaq93JVQ8+uejeiSZ/iTpc2Pl8Nu6Cgal6EqorzneKcGJBiv5iQzKXmXlCVo/mQ422ar1fEs/" +
            "8TApLfmclfsmrgRmXYT7dQFMjQo1BNQOJODIHLjt5QIKgmBNnrbueGYOqX1j0F0Bzpurz+JAl36wN5Gx" +
            "KOlK2wtA6IZx/5dgFC0rUdidlj+LnB3V8f8pLIJWBTSzT4h5xWFMYY8Nn0q5GPrUj+s+fMOgc3xBYGkX" +
            "rXMX4v/3XdlCt7UZTpHwFCquKHTrcq3j1TDXyEJ/6hlLPM35e6fdAHtC0b9IQFYUPUO/FJsO/lSwcw9V" +
            "FTrSjetKUsXpSvlI1doif558Z9qDPu1ecfrEo+ow+KFSvITKv6KfrJ8i7HN4SVTHUQZOK49OuuwReVgV" +
            "8Ti6i7vJNwPw35csoDUOv/Wwk0yFX6tL+g8HE2UuNDQhcB5d9o0xoFwphCb+vQs2kQXc//oevUMX9cv3" +
            "87MHEI7np/gUaneMpHaVaQ==";

            Hasp hasp = new Hasp(feature);
            HaspStatus status = hasp.Login(vendorCode);

            if (HaspStatus.StatusOk != status)
            {
                // Application.Current.Shutdown(0);
                logManager.Error("License Key Not Found.");
                MessageBox.Show("라이센스 키를 찾을 수 없어 프로그램을 종료합니다.", "경고", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(0);
            }

            cameraManager = new HCameraManager(500);
            // RefreshCameraList();

            configIni = new IniFile(AppDomain.CurrentDomain.BaseDirectory + "Config.ini");

            imageSaveThread = new Thread(ImageSaveThreadDo);

            messageCollection = new ObservableCollection<MessageData>();

            this.window = window;

            new Thread(new ThreadStart(() =>
            {
                while (true)
                {
                    this.window.Dispatcher.Invoke(() =>
                    {
                        NotifyPropertyChanged("SendWaitImages");
                    });
                    Thread.Sleep(300);
                }

            }));

            // InitServer();
        }

        public void RefreshCameraList()
        {
            List<HCamera> list = cameraManager.LoadCameras();

            // 불러온 카메라 리스트 중에 없는 카메라 제거
            for (int i = 0; i < cameraCollection.Count; i++)
            {
                HCamera camera = cameraCollection[i];
                bool isChecked = false;

                for (int j = 0; j < list.Count; j++)
                {
                    HCamera temp = list[j];

                    if (camera.Info.SerialNumber == temp.Info.SerialNumber)
                    {
                        isChecked = true;
                    }
                }

                if (!isChecked)
                {
                    CameraCollection.RemoveAt(i);
                    i--;
                }
            }

            // 불러온 카메라 리스트 중에 없는 카메라 추가
            for (int i = 0; i < list.Count; i++)
            {
                HCamera temp = list[i];
                bool isChecked = false;

                for (int j = 0; j < cameraCollection.Count; j++)
                {
                    HCamera camera = cameraCollection[j];

                    if (camera.Info.SerialNumber == temp.Info.SerialNumber)
                    {
                        isChecked = true;
                    }
                }

                if (!isChecked)
                {
                    CameraCollection.Add(temp);
                }
            }

            NumberingCameraList();
            
            // CameraCollection = new ObservableCollection<HCamera>(list);
        }

        private void NumberingCameraList()
        {
            for (int i = 0; i < cameraCollection.Count; i++)
            {
                cameraCollection[i].Info.DeviceNumber = Convert.ToUInt32(i + 1);
            }
        }

        public void UpdateDeviceList()
        {
            try
            {
                /* Ask the device enumerator for a list of devices. */
                // List<DeviceEnumerator.Device> list = DeviceEnumerator.EnumerateDevices();
                List<HCamera> list = cameraManager.LoadCameras();

                CameraCollection = new ObservableCollection<HCamera>();
                list.ForEach(x =>
                {
                    CameraCollection.Add(x);
                });
            }
            catch (Exception e)
            {
                logManager.Error(e.Message);
            }
        }

        public void LoadParams()
        {
            if (selectedCamera != null)
            {
                Exposure = Convert.ToInt32(selectedCamera.Parameters.Exposure);
                ExposureMin = Convert.ToInt32(selectedCamera.Parameters.ExposureMin);
                ExposureMax = Convert.ToInt32(selectedCamera.Parameters.ExposureMax);
                ExposureInterval = Convert.ToInt32(selectedCamera.Parameters.ExposureInterval);
            }
        }

        public void Stop()
        {
            try
            {
                if (selectedCameraList.Count > 0)
                {
                    for (int i = 0; i < selectedCameraList.Count; i++)
                    {
                        selectedCameraList[i].StopContinousShot();
                        selectedCameraList[i].StopGrab();
                    }
                }
            }
            catch (Exception e)
            {
                logManager.Error(e.Message);
            }
        }

        public void Close()
        {
            try
            {

                for (int i = 0; i < selectedCameraList.Count; i++)
                {
                    if (selectedCameraList[i] != null)
                    {
                        selectedCameraList[i].Close();
                        ShowConsole("카메라 접속 해제 : " + selectedCameraList[i].Info.SerialNumber, DateTime.Now);
                        selectedCameraList[i] = null;
                        selectedCameraList.RemoveAt(i);
                        i--;
                    }
                }

                /*
                if (selectedCamera != null)
                {
                    selectedCamera.Close();
                }

                selectedCamera = null;
                */
            }
            catch (Exception e)
            {
                logManager.Error(e.Message);
            }
        }

        public void OneShot()
        {
            if (selectedCameraList.Count > 0)
            {
                for (int i = 0; i < selectedCameraList.Count; i++)
                {
                    try
                    {
                        HCamera camera = selectedCameraList[i];

                        string dir = ImageSavePath + "\\" + DateTime.Now.ToString("yyyy-MM-dd") + "\\";

                        if (camera.Info.DeviceNumber == 1)
                        {
                            dir += "CAM1\\OneShot";
                        }
                        else if (camera.Info.DeviceNumber == 2)
                        {
                            dir += "CAM2\\OneShot";
                        }

                        if (!Directory.Exists(dir))
                        {
                            Directory.CreateDirectory(dir);
                        }

                        camera.OneShot(dir);
                        ShowConsole("단일 촬영 " + camera.Info.SerialNumber, DateTime.Now);
                    }
                    catch (Exception e)
                    {
                        logManager.Error(e.Message);
                    }
                }
            }
        }

        public void ContinuousShot()
        {
            if (selectedCameraList.Count > 0)
            {
                for (int i = 0; i < selectedCameraList.Count; i++)
                {
                    try
                    {
                        HCamera camera = selectedCameraList[i];

                        string dir = ImageSavePath + "\\" + DateTime.Now.ToString("yyyy-MM-dd") + "\\";

                        if (camera.Info.DeviceNumber == 1)
                        {
                            dir += "CAM1\\ContinousShot";
                        }
                        else if (camera.Info.DeviceNumber == 2)
                        {
                            dir += "CAM2\\ContinousShot";
                        }

                        if (!Directory.Exists(dir))
                        {
                            Directory.CreateDirectory(dir);
                        }

                        if (useCaptureLimit)
                        {
                            camera.ContinousShot(dir, captureLimit);
                        }
                        else
                        {
                            camera.ContinousShot(dir);
                        }
                        
                        ShowConsole("연속 촬영 " + camera.Info.SerialNumber, DateTime.Now);
                    }
                    catch (Exception e)
                    {
                        logManager.Error(e.Message);
                    }
                }
            }
        }

        public void StopContinuousShot()
        {
            if (selectedCameraList.Count > 0)
            {
                for (int i = 0; i < selectedCameraList.Count; i++)
                {
                    try
                    {
                        HCamera camera = selectedCameraList[i];

                        camera.StopContinousShot();
                        ShowConsole("연속 촬영 정지 " + camera.Info.SerialNumber, DateTime.Now);
                    }
                    catch (Exception e)
                    {
                        logManager.Error(e.Message);
                    }
                }
            }
        }

        private float calcBlurriness(Mat src)
        {
            Mat gx = new Mat();
            Mat gy = new Mat();
            Cv2.Sobel(src, gx, MatType.CV_32F, 1, 0);
            Cv2.Sobel(src, gy, MatType.CV_32F, 0, 1);
            double normGX = Cv2.Norm(gx);
            double normGy = Cv2.Norm(gy);
            double sumSq = normGX * normGX + normGy * normGy;
            gx.Dispose();
            gy.Dispose();
            return (float)(1.0 / (sumSq / (src.Size().Height * src.Size().Width) + 1e-6)) * 1000;
        }

        private void OnImageReadyEventCallback()
        {
            
        }


        private void OnDeviceOpenedEventCallback()
        {

        }


        private void ShowException(Exception e, string additionalErrorMessage)
        {

            string more = "\n\nLast error message (may not belong to the exception):\n" + additionalErrorMessage;
            logManager.Error("Exception caught:\n" + e.Message + (additionalErrorMessage.Length > 0 ? more : ""));
        }

        public void ShowConsole(string message, DateTime dateTime)
        {
            OnMessageReceiveEvent(message, dateTime, "일반");
        }

        private readonly object imageSaveLockObj = new object();

        private void ImageSaveThreadDo()
        {
            while (!flagStopImageSaveThread)
            {
                try
                {
                    lock (imageSaveLockObj)
                    {
                        if (selectedCameraList.Count > 0)
                        {
                            for (int i = 0; i < selectedCameraList.Count; i++)
                            {
                                HCamera camera = selectedCameraList[i];

                                if (camera.GrabImages.Count > 0)
                                {
                                    HGrabImage grabImage = camera.GrabImages.Dequeue();

                                    string dir = ImageSavePath + "\\" + DateTime.Now.ToString("yyyy-MM-dd") + "\\";

                                    if (grabImage.Type == HGrabImage.ShotType.OneShot)
                                    {
                                        if (camera.Info.DeviceNumber == 1)
                                        {
                                            dir += "CAM1\\OneShot";
                                        }
                                        else if (camera.Info.DeviceNumber == 2)
                                        {
                                            dir += "CAM2\\OneShot";
                                        }
                                    }
                                    else if (grabImage.Type == HGrabImage.ShotType.ContinousShot)
                                    {
                                        if (camera.Info.DeviceNumber == 1)
                                        {
                                            dir += "CAM1\\ContinousShot";
                                        }
                                        else if (camera.Info.DeviceNumber == 2)
                                        {
                                            dir += "CAM2\\ContinousShot";
                                        }
                                    }

                                    if (!Directory.Exists(dir))
                                    {
                                        Directory.CreateDirectory(dir);
                                    }

                                    grabImage.Bitmap.Save(dir + grabImage.Name + ".bmp");
                                }
                            }
                        }
                    }
                    

                    Thread.Sleep(1);
                }
                catch
                {

                }
            }
        }

        public void StartImageSaveThread()
        {
            flagStopImageSaveThread = false;
            imageSaveThread.Start();
            // ImageSaveThread2.Start();
        }

        public void StopImageSaveThread()
        {
            flagStopImageSaveThread = true;
            imageSaveThread.Join();
            // ImageSaveThread2.Join();
        }

        public BitmapSource ToWpfBitmap(Bitmap bitmap)
        {
            BitmapData bitmapData = bitmap.LockBits(
                new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
                System.Drawing.Imaging.ImageLockMode.ReadOnly, bitmap.PixelFormat);

            BitmapSource bitmapSource = null;

            if (bitmap.PixelFormat == PixelFormat.Format8bppIndexed)
            {
                bitmapSource = BitmapSource.Create(
                    bitmapData.Width, bitmapData.Height,
                    bitmap.HorizontalResolution, bitmap.VerticalResolution,
                    System.Windows.Media.PixelFormats.Gray8, null,
                    bitmapData.Scan0, bitmapData.Stride * bitmapData.Height, bitmapData.Stride
                );
            }
            else
            {
                bitmapSource = BitmapSource.Create(
                    bitmapData.Width, bitmapData.Height,
                    bitmap.HorizontalResolution, bitmap.VerticalResolution,
                    System.Windows.Media.PixelFormats.Pbgra32, null,
                    bitmapData.Scan0, bitmapData.Stride * bitmapData.Height, bitmapData.Stride
                );
            }

            bitmap.UnlockBits(bitmapData);
            bitmapSource.Freeze();
            return bitmapSource;
        }

        public Bitmap ToBitmap(BitmapSource bitmapSource)
        {
            Bitmap bitmap = null;

            using (MemoryStream ms = new MemoryStream())
            {
                BitmapEncoder bitmapEncoder = new BmpBitmapEncoder();
                bitmapEncoder.Frames.Add(BitmapFrame.Create(bitmapSource));
                bitmapEncoder.Save(ms);

                bitmap = new Bitmap(ms);
            }

            return bitmap;
        }

        public void SaveBitmapSource(string path, BitmapSource bitmapSource)
        {
            if (bitmapSource != null)
            {
                using (FileStream fs = new FileStream(path, FileMode.Create))
            {
                try
                {
                    string extension = Path.GetExtension(path);
                    BitmapEncoder bitmapEncoder = null;

                    if (extension.Contains("bmp"))
                    {
                        bitmapEncoder = new BmpBitmapEncoder();
                    }
                    else if (extension.Contains("jpg") || extension.Contains("jpeg"))
                    {
                        bitmapEncoder = new JpegBitmapEncoder();
                    }

                    if (bitmapEncoder != null)
                    {
                        bitmapEncoder.Frames.Add(BitmapFrame.Create(bitmapSource));
                        bitmapEncoder.Save(fs);
                    }
                }
                catch (Exception e)
                {
                    logManager.Error("SaveBitmapSource Failed. " + e.Message);
                }
            }
            }
        }

        public void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key.ToString() == Start_ContinuousShot_Key.Trim())
            {
                ContinuousShot();
            }

            if (e.Key.ToString() == Stop_ContinuousShot_Key.Trim())
            {
                StopContinuousShot();
            }
        }

        public void ChangeImageResultPathButton_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog openFileDialog = new CommonOpenFileDialog();
            openFileDialog.IsFolderPicker = true;

            if (openFileDialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                ImageSavePath = openFileDialog.FileName;
            }
        }
    }
}
