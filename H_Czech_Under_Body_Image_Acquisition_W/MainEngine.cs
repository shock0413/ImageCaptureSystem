using H_Czech_Under_Body_Image_Acquisition_W.Struct;
using MahApps.Metro;
using MahApps.Metro.Converters;
using PylonC.NET;
using PylonC.NETSupportLibrary;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace H_Czech_Under_Body_Image_Acquisition_W
{
    public class MainEngine : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }

        public delegate void OnMessageReceiveEventHander(string message, DateTime dateTime, string type);
        public event OnMessageReceiveEventHander OnMessageReceiveEvent = delegate { };

        public ObservableCollection<CameraDevice> CameraCollection { get { return cameraCollection; } set { cameraCollection = value; NotifyPropertyChanged("CameraCollection"); } }
        public ObservableCollection<CameraDevice> cameraCollection;

        public ObservableCollection<MessageData> MessageCollection { get { return messageCollection; } set { messageCollection = value; NotifyPropertyChanged("MessageCollection"); } }
        public ObservableCollection<MessageData> messageCollection;

        public object lockSaveObj = new object();
        public Thread ImageSaveThread1;
        public Thread ImageSaveThread2;
        /*
        public Thread ImageSaveThread3;
        public Thread ImageSaveThread4;
        */
        public bool flagStopImageSaveThread = false;

        public readonly object lockConvertObj = new object();
        public Thread ImageConvertThread;
        public bool flagStopImageConvertThread = false;

        public readonly object lockMemoryCheckObj = new object();
        public Thread MemoryCheckThread;
        public bool flagStopMemoryCheckThread = false;

        private ImageProvider m_imageProvider = new ImageProvider();
        private Bitmap m_bitmap = null;

        public readonly object lockDisplayObj = new object();
        public BitmapSource DisplayImage { get { return displayImage; } set { lock (lockDisplayObj) { displayImage = value; NotifyPropertyChanged("DisplayImage"); } } }
        private BitmapSource displayImage;

        public int CameraFPS { get { return cameraFPS; } set { cameraFPS = value; NotifyPropertyChanged("CameraFPS"); } }
        private int cameraFPS;

        public Visibility CameraFPS_Visibility { get { return cameraFPS_Visibility; } set { cameraFPS_Visibility = value; NotifyPropertyChanged("CameraFPS_Visibility"); } }
        private Visibility cameraFPS_Visibility = Visibility.Hidden;

        public int DisplayFPS { get { return displayFPS; } set { displayFPS = value; NotifyPropertyChanged("DisplayFPS"); } }
        private int displayFPS;

        public Visibility DisplayFPS_Visibility { get { return displayFPS_Visibility; } set { displayFPS_Visibility = value; NotifyPropertyChanged("DisplayFPS_Visibility"); } }
        private Visibility displayFPS_Visibility = Visibility.Hidden;

        public int SaveFPS { get { return saveFPS; } set { saveFPS = value; NotifyPropertyChanged("SaveFPS"); } }
        private int saveFPS;

        public Visibility SaveFPS_Visibility { get { return saveFPS_Visibility; } set { saveFPS_Visibility = value; NotifyPropertyChanged("SaveFPS_Visibility"); } }
        private Visibility saveFPS_Visibility = Visibility.Hidden;

        public int SaveRemainCount { get { return saveRemainCount; } set { saveRemainCount = value; NotifyPropertyChanged("SaveRemainCount"); } }
        private int saveRemainCount;

        public Visibility SaveRemainCount_Visibility { get { return saveRemainCount_Visibility; } set { saveRemainCount_Visibility = value; NotifyPropertyChanged("SaveRemainCount_Visibility"); } }
        private Visibility saveRemainCount_Visibility = Visibility.Hidden;

        public CameraDevice CurrentCamera
        {
            get { return currentCamera; }
            set
            {
                currentCamera = value;

                if (currentCamera != null)
                {
                    IsSelectedCamera = true;
                }
                else
                {
                    IsSelectedCamera = false;
                }

                if (currentCamera.Device.DeviceClass.Equals("BaslerGigE"))
                {
                    PacketSizeVisi = Visibility.Visible;
                    InterPacketVisi = Visibility.Visible;
                    SensorReadoutModeVisi = Visibility.Collapsed;
                }
                else if (currentCamera.Device.DeviceClass.Equals("BaslerUsb"))
                {
                    PacketSizeVisi = Visibility.Collapsed;
                    InterPacketVisi = Visibility.Collapsed;
                    SensorReadoutModeVisi = Visibility.Visible;
                    m_imageProvider.SetSequencerMode(false);
                }

                string[] split = currentCamera.Name.Split('-');
                string[] frameStr = split[split.Length - 1].Split(' ');

                string s = "";
                char[] array = frameStr[0].ToArray();

                for (int i = 0; i < array.Length; i++)
                {
                    int n;
                    if (int.TryParse(array[i].ToString(), out n))
                    {
                        s += array[i].ToString();
                    }
                }

                FrameRateMax = Convert.ToInt32(s);

                NotifyPropertyChanged("CurrentCamera");
            }
        }
        public CameraDevice currentCamera;

        public bool IsSelectedCamera { get { return isSelectedCamera; } set { isSelectedCamera = value; NotifyPropertyChanged("IsSelectedCamera"); } }
        private bool isSelectedCamera;

        private bool isContinousShot = false;

        public int Gain { get { return gain; } set { gain = value; NotifyPropertyChanged("Gain"); m_imageProvider.SetGain(value, CurrentCamera.Device); } }
        private int gain = 300;

        public int GainMin { get { return gainMin; } set { gainMin = value; NotifyPropertyChanged("GainMin"); } }
        private int gainMin = 0;

        public int GainMax { get { return gainMax; } set { gainMax = value; NotifyPropertyChanged("GainMax"); } }
        private int gainMax;

        public int Exposure { get { return exposure; } set { exposure = value; NotifyPropertyChanged("Exposure"); m_imageProvider.SetExposure(value, CurrentCamera.Device); } }
        private int exposure = 16;

        public int ExposureMin { get { return exposureMin; } set { exposureMin = value; NotifyPropertyChanged("ExposureMin"); } }
        private int exposureMin;

        public int ExposureMax { get { return exposureMax; } set { exposureMax = value; NotifyPropertyChanged("ExposureMax"); } }
        private int exposureMax;

        public int FrameRate { get { return frameRate; } set { frameRate = value; NotifyPropertyChanged("FrameRate"); NotifyPropertyChanged("CaptureSpeed"); m_imageProvider.SetFrameRate(value, CurrentCamera.Device); } }
        private int frameRate;

        public int FrameRateMin { get { return frameRateMin; } set { frameRateMin = value; NotifyPropertyChanged("FrameRateMin"); } }
        private int frameRateMin = 1;

        public int FrameRateMax { get { return frameRateMax; } set { frameRateMax = value; NotifyPropertyChanged("FrameRateMax"); } }
        private int frameRateMax;

        public long PacketSize { get { return packetSize; } set { packetSize = value; m_imageProvider.SetPacketSize(value); NotifyPropertyChanged("PacketSize"); } }
        private long packetSize;

        public Visibility PacketSizeVisi { get { return packetSizeVisi; } set { packetSizeVisi = value; NotifyPropertyChanged("PacketSizeVisi"); } }
        private Visibility packetSizeVisi;

        public long InterPacketDelay { get { return interPacketDelay; } set { interPacketDelay = value; m_imageProvider.SetInterPacketDelay(value); NotifyPropertyChanged("InterPacketDelay"); } }
        private long interPacketDelay;

        public Visibility InterPacketVisi { get { return interPacketVisi; } set { interPacketVisi = value; NotifyPropertyChanged("InterPacketVisi"); } }
        private Visibility interPacketVisi;

        public Visibility SensorReadoutModeVisi { get { return sensorReadoutModeVisi; } set { sensorReadoutModeVisi = value; NotifyPropertyChanged("SensorReadoutModeVisi"); } }
        private Visibility sensorReadoutModeVisi;

        public string SensorReadoutMode { get { return sensorReadoutMode; } set {  } }
        private string sensorReadoutMode = "";

        public bool IsSensorReadoutNormal { get { return isSensorReadoutNormal; } set { if (isContinousShot) { return; } isSensorReadoutNormal = value; NotifyPropertyChanged("IsSensorReadoutNormal"); if (isSensorReadoutNormal) { m_imageProvider.SetSensorReadoutMode("Normal"); } } }
        private bool isSensorReadoutNormal;

        public bool IsSensorReadoutFast { get { return isSensorReadoutFast; } set { if (isContinousShot) { return; } isSensorReadoutFast = value; NotifyPropertyChanged("IsSensorReadoutFast"); if (isSensorReadoutFast) { m_imageProvider.SetSensorReadoutMode("Fast"); } } }
        private bool isSensorReadoutFast;

        public double CaptureSpeed
        {
            get
            {
                try
                {
                    return Math.Round((1000.0 / FrameRate));
                }
                catch
                {
                    return 0;
                }
            }

            set
            {
                FrameRate = Convert.ToInt32(1000.0 / value);
            }
        }

        public bool EnableFrameRate { get { return enableFrameRate; } set { enableFrameRate = value; NotifyPropertyChanged("EnableFrameRate"); m_imageProvider.SetFrameRateEnable(value); } }
        public bool enableFrameRate = false;

        public ICommand WhiteBalanceCmd { get { return (whiteBalance) ?? (whiteBalance = new DelegateCommand(SetWhiteBalance)); } }
        private ICommand whiteBalance;

        public void SetWhiteBalance()
        {
            m_imageProvider.SetWhiteBalance();
        }

        public ICommand OpenImagePathCmd { get { return (openImagePath) ?? (openImagePath = new DelegateCommand(OpenImagePath)); } }
        private ICommand openImagePath;

        public void OpenImagePath()
        {
            Process.Start(ImageSavePath);
        }

        public bool EnablePacketSize { get { return enablePacketSize; } set { enablePacketSize = value; NotifyPropertyChanged("EnablePacketSize"); } }
        private bool enablePacketSize;

        public string ImageSavePath { get { return imageSavePath; } set { imageSavePath = value; NotifyPropertyChanged("ImageSavePath"); configIni.WriteValue("Setting", "ImageSavePath", value); } }
        private string imageSavePath;

        public string Start_ContinuousShot_Key { get { return configIni.GetString("ShortCut_Key", "Start_ContinuousShot_Key", "A"); } }

        public string Stop_ContinuousShot_Key { get { return configIni.GetString("ShortCut_Key", "Stop_ContinuousShot_Key", "S"); } }

        Queue<ImageData> SaveBitmapQueue = new Queue<ImageData>();
        Queue<ImageData> ConvertBitmapQueue = new Queue<ImageData>();

        public int CaptureCount { get; set; }

        public int DisplayCount { get; set; }

        public int SaveCount { get; set; }

        public int MinCaptureIntval { get { return minCaptureIntval; } set { minCaptureIntval = value; if (value != int.MaxValue) { NotifyPropertyChanged("MinCaptureIntval"); } } }
        private int minCaptureIntval = int.MaxValue;

        public Visibility MinCaptureIntvalVt { get { return minCaptureIntvalVt; } set { minCaptureIntvalVt = value; NotifyPropertyChanged("MinCaptureIntvalVt"); } }
        private Visibility minCaptureIntvalVt = Visibility.Hidden;

        public int MaxCaptureIntval { get { return maxCaptureIntval; } set { maxCaptureIntval = value; if (value != int.MinValue) { NotifyPropertyChanged("MaxCaptureIntval"); } } }
        private int maxCaptureIntval = int.MinValue;

        public Visibility MaxCaptureIntvalVt { get { return maxCaptureIntvalVt; } set { maxCaptureIntvalVt = value; NotifyPropertyChanged("MaxCaptureIntvalVt"); } }
        private Visibility maxCaptureIntvalVt = Visibility.Hidden;

        public int AvgCaptureIntval { get { return avgCaptureIntval; } set { avgCaptureIntval = value; NotifyPropertyChanged("AvgCaptureIntval"); } }
        private int avgCaptureIntval = 0;

        public Visibility AvgCaptureIntvalVt { get { return avgCaptureIntvalVt; } set { avgCaptureIntvalVt = value; NotifyPropertyChanged("AvgCaptureIntvalVt"); } }
        private Visibility avgCaptureIntvalVt = Visibility.Hidden;

        public int TotalCaptureIntval { get { return totalCaptureIntval; } set { totalCaptureIntval = value; } }
        private int totalCaptureIntval = 0;

        public int CaptureLimit { get { return captureLimit; } set { captureLimit = value; NotifyPropertyChanged("CaptureLimit"); } }
        private int captureLimit = 1;

        public bool UseCaptureLimit { get { return useCaptureLimit; } set { useCaptureLimit = value; NotifyPropertyChanged("UseCaptureLimit"); } }
        private bool useCaptureLimit;

        public bool UseSaveMode { get { return useSaveMode; } set { useSaveMode = value; NotifyPropertyChanged("UseSaveMode"); } }
        private bool useSaveMode = false;

        public bool FlagStopContinuousLimitShot = false;

        private string ContinuousCaptureTitle = null;

        public long MemoryUsage { get { return memoryUsage; } set { memoryUsage = value; NotifyPropertyChanged("MemoryUsage"); } }
        private long memoryUsage = 0;

        public System.Windows.Media.Brush MemoryCheckColor { get { return memoryCheckColor; } set { memoryCheckColor = value; NotifyPropertyChanged("MemoryCheckColor"); } }
        private System.Windows.Media.Brush memoryCheckColor;

        public Visibility WarnningMsgVsi { get { return warnningMsgVsi; } set { warnningMsgVsi = value; NotifyPropertyChanged("WarnningMsgVsi"); } }
        private Visibility warnningMsgVsi;

        public string WarnningMsg { get { return warnningMsg; } set { warnningMsg = value; NotifyPropertyChanged("WarnningMsg"); } }
        private string warnningMsg;

        private bool IsMemoryUsageFull { get; set; }

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

        Process[] procs = Process.GetProcessesByName("H_Czech_Under_Body_Image_Acquisition_W");

        public MainEngine()
        {
            // 이미 실행되어 있을 시 종료
            if (procs.Length > 1)
            {
                Application.Current.Shutdown(0);
            }

            m_imageProvider.ImageReadyEvent += new ImageProvider.ImageReadyEventHandler(OnImageReadyEventCallback);
            m_imageProvider.GrabErrorEvent += new ImageProvider.GrabErrorEventHandler(OnGrabErrorEventCallback);
            m_imageProvider.DeviceOpenedEvent += new ImageProvider.DeviceOpenedEventHandler(OnDeviceOpenedEventCallback);

            configIni = new IniFile("Config.ini");
            ImageSavePath = configIni.GetString("Setting", "ImageSavePath", "Result\\");

            ImageSaveThread1 = new Thread(new ThreadStart(() =>
            {
                Parallel.Invoke(() =>
                {
                    ImageSaveThreadDo();
                });
            }));
            ImageSaveThread2 = new Thread(new ThreadStart(ImageSaveThreadDo));
            /*
            ImageSaveThread3 = new Thread(new ThreadStart(ImageSaveThreadDo));
            ImageSaveThread4 = new Thread(new ThreadStart(ImageSaveThreadDo));
            */

            ImageConvertThread = new Thread(new ThreadStart(ImageConvertThreadDo));

            MemoryCheckThread = new Thread(new ThreadStart(MemoryCheck));

            messageCollection = new ObservableCollection<MessageData>();
        }

        public void UpdateDeviceList()
        {
            try
            {
                /* Ask the device enumerator for a list of devices. */
                List<DeviceEnumerator.Device> list = DeviceEnumerator.EnumerateDevices();

                CameraCollection = new ObservableCollection<CameraDevice>();
                list.ForEach(x =>
                {
                    CameraCollection.Add(new CameraDevice(x));
                });
            }
            catch
            {
                
            }
        }

        public void Open(CameraDevice cameraDevice)
        {
            try
            {
                m_imageProvider.Open(cameraDevice.Index);

                CurrentCamera = cameraDevice;

                ShowConsole("카메라 접속 성공 : " + currentCamera.FullName, DateTime.Now);

                LoadParams();
            }
            catch (Exception e)
            {
                ShowException(e, "카메라 접속 실패!");
            }
        }

        public void LoadParams()
        {
            Gain = (int)m_imageProvider.GetGain(CurrentCamera.Device);
            GainMin = (int)m_imageProvider.GetGainMin(CurrentCamera.Device);
            GainMax = (int)m_imageProvider.GetGainMax(CurrentCamera.Device);
            Exposure = (int)m_imageProvider.GetExposure(CurrentCamera.Device);
            ExposureMin = (int)m_imageProvider.GetExposureMin(CurrentCamera.Device);
            ExposureMax = (int)m_imageProvider.GetExposureMax(CurrentCamera.Device);
            FrameRate = (int)m_imageProvider.GetFrameRate(CurrentCamera.Device);
            EnableFrameRate = m_imageProvider.GetFrameRateEnable();

            if (CurrentCamera.Device.DeviceClass.Equals("BaslerGigE"))
            {
                PacketSize = m_imageProvider.GetPacketSize();
                InterPacketDelay = m_imageProvider.GetInterPacketDelay();
            }

            string sensorReadoutModeStr = m_imageProvider.GetSensorReadoutMode();

            if (sensorReadoutModeStr.Equals("Normal"))
            {
                IsSensorReadoutFast = false;
                IsSensorReadoutNormal = true;
            }
            else if (sensorReadoutModeStr.Equals("Fast"))
            {
                IsSensorReadoutNormal = false;
                IsSensorReadoutFast = true;
            }
        }

        public void Stop()
        {
            try
            {
                m_imageProvider.Stop();
            }
            catch
            {
                
            }
        }

        public void Close()
        {
            try
            {
                m_imageProvider.Close();

                if (currentCamera != null)
                {
                    ShowConsole("카메라 접속 해제 : " + currentCamera.FullName, DateTime.Now);
                }

                CurrentCamera = null;
            }
            catch
            {
                
            }
        }

        public void OneShot()
        {
            ContinuousCaptureTitle = DateTime.Now.ToString("HHmmss");

            try
            {
                m_imageProvider.OneShot();
                ShowConsole("단일 촬영", DateTime.Now);
            }
            catch
            {
                
            }
        }

        private DateTime startTime;
        private DateTime capturePrevTime;
        private DateTime captureNextTime;
        private DateTime savePrevTime;
        private DateTime saveNextTime;
        private DateTime displayPrevTime;
        private DateTime displayNextTime;

        public void ContinuousShot()
        {
            ShowConsole("연속 촬영 시작", DateTime.Now);

            ContinuousCaptureTitle = DateTime.Now.ToString("HHmmss");

            startTime = DateTime.Now;
            capturePrevTime = startTime;

            isContinousShot = true;

            CameraFPS_Visibility = Visibility.Visible;

            AvgCaptureIntval = 0;
            MinCaptureIntval = int.MaxValue;
            MaxCaptureIntval = int.MinValue;

            AvgCaptureIntvalVt = Visibility.Visible;
            MinCaptureIntvalVt = Visibility.Visible;
            MaxCaptureIntvalVt = Visibility.Visible;

            new Thread(new ThreadStart(() =>
            {
                try
                {
                    m_imageProvider.ContinuousShot();

                    CaptureCount = 0;
                }
                catch
                {
                    
                }
            })).Start();
        }


        public void StopContinuousShot()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                ShowConsole("연속 촬영 중지", DateTime.Now);
            });

            isContinousShot = false;

            CameraFPS_Visibility = Visibility.Hidden;

            AvgCaptureIntvalVt = Visibility.Hidden;
            MinCaptureIntvalVt = Visibility.Hidden;
            MaxCaptureIntvalVt = Visibility.Hidden;

            new Thread(new ThreadStart(() =>
            {
                try
                {
                    m_imageProvider.Stop();
                    // stopWatch.Stop();
                }
                catch (Exception e)
                {
                    System.Diagnostics.StackTrace trace = new System.Diagnostics.StackTrace(e, true);
                    MessageBox.Show(trace + Environment.NewLine + e.Message);
                }
            })).Start();
        }

        private void OnImageReadyEventCallback()
        {
            try
            {
                lock (lockConvertObj)
                {
                    ImageProvider.Image image = m_imageProvider.GetLatestImage();

                    if (image != null)
                    {
                        DateTime now = DateTime.Now;

                        TimeSpan intval = now.Subtract(captureNextTime);

                        int ms_intval = intval.Milliseconds;

                        TotalCaptureIntval += ms_intval;

                        if (MinCaptureIntval > ms_intval)
                        {
                            MinCaptureIntval = ms_intval;
                        }

                        if (MaxCaptureIntval < ms_intval)
                        {
                            MaxCaptureIntval = ms_intval;
                        }

                        captureNextTime = now;

                        // 총 경과 시간
                        TimeSpan totalTime = captureNextTime.Subtract(startTime);

                        // 현재 촬영 시간 - 이전 촬영 시간
                        TimeSpan subtime = captureNextTime.Subtract(capturePrevTime);

                        CaptureCount++;

                        if (subtime.Seconds == 1 && subtime.Milliseconds >= 0)
                        {
                            AvgCaptureIntval = TotalCaptureIntval / CaptureCount;
                            CameraFPS = CaptureCount;
                            CaptureCount = 0;
                            TotalCaptureIntval = 0;
                            capturePrevTime = captureNextTime;

                            MinCaptureIntval = int.MaxValue;
                            MaxCaptureIntval = int.MinValue;
                        }

                        // 제한된 시간이 지나면 종료
                        if (UseCaptureLimit && totalTime.TotalSeconds >= CaptureLimit && totalTime.Milliseconds >= 0)
                        {
                            StopContinuousShot();
                        }

                        // 큐가 비어 있을 때 에러 뜨는지 확인
                        // https://blog.naver.com/bererere/220759418904
                        if (startTime <= captureNextTime)
                        {
                            ConvertBitmapQueue.Enqueue(new ImageData() { Image = image, DateTime = captureNextTime, Name = captureNextTime.ToString("HH_mm_ss.fff") });
                        }

                        if (ConvertBitmapQueue.Count == 1 && SaveBitmapQueue.Count == 0)
                        {
                            displayPrevTime = DateTime.Now;
                        }
                    }
                }
            }
            catch
            {

            }
        }

        private void OnGrabErrorEventCallback(Exception grabException, string additionalErrorMessage)
        {

            ShowException(grabException, additionalErrorMessage);
        }


        private void OnDeviceOpenedEventCallback()
        {

        }


        private void ShowException(Exception e, string additionalErrorMessage)
        {
            string more = "\n\nLast error message (may not belong to the exception):\n" + additionalErrorMessage;
            MessageBox.Show("Exception caught:\n" + e.Message + (additionalErrorMessage.Length > 0 ? more : ""), "Error");
        }

        private void ShowConsole(string message, DateTime dateTime)
        {
            OnMessageReceiveEvent(message, dateTime, "일반");
        }

        public void StartImageSaveThread()
        {
            flagStopImageSaveThread = false;
            ImageSaveThread1.Start();
            ImageSaveThread2.Start();
            /*
            ImageSaveThread3.Start();
            ImageSaveThread4.Start();
            */
        }

        public void StopImageSaveThread()
        {
            flagStopImageSaveThread = true;
            ImageSaveThread1.Join();
            ImageSaveThread2.Join();
            /*
            ImageSaveThread3.Join();
            ImageSaveThread4.Join();
            */
        }

        public void StartImageConvertThread()
        {
            flagStopImageConvertThread = false;
            ImageConvertThread.Start();
        }

        public void StopImageConvertThread()
        {
            flagStopImageConvertThread = true;
            ImageConvertThread.Join();
        }

        public void StartMemoryCheckThread()
        {
            flagStopMemoryCheckThread = false;
            MemoryCheckThread.Start();
        }

        public void StopMemoryCheckThread()
        {
            flagStopMemoryCheckThread = true;
            MemoryCheckThread.Join();
        }

        public void ImageSaveThreadDo()
        {
            while (!flagStopImageSaveThread)
            {
                if (SaveBitmapQueue.Count > 0)
                {
                    try
                    {
                        lock (lockSaveObj)
                        {
                            ImageData imageData = SaveBitmapQueue.Dequeue();

                            string dir = ImageSavePath + "\\" + imageData.DateTime.ToString("yyyy-MM-dd") + "\\" + ContinuousCaptureTitle;
                            string path = dir + "\\" + imageData.Name + ".bmp";

                            if (!Directory.Exists(dir))
                            {
                                Directory.CreateDirectory(dir);
                            }

                            try
                            {
                                imageData.Bitmap.Save(path, ImageFormat.Bmp);
                            }
                            catch
                            {
                                
                            }

                            if (SaveFPS_Visibility == Visibility.Hidden)
                            {
                                SaveFPS_Visibility = Visibility.Visible;
                            }

                            SaveCount++;

                            saveNextTime = DateTime.Now;
                            TimeSpan subtime = saveNextTime.Subtract(savePrevTime);

                            if (subtime.Seconds == 1 && subtime.Milliseconds >= 0)
                            {
                                SaveFPS = SaveCount;
                                SaveCount = 0;
                                savePrevTime = saveNextTime;
                            }

                            if (SaveRemainCount_Visibility == Visibility.Hidden)
                            {
                                SaveRemainCount_Visibility = Visibility.Visible;
                            }

                            SaveRemainCount = SaveBitmapQueue.Count;

                            imageData.Bitmap.Dispose();
                            imageData.Bitmap = null;
                            imageData.Image = null;
                            imageData = null;

                            GC.Collect();
                        }
                    }
                    catch
                    {
                        
                    }
                }
                else
                {
                    if (SaveFPS_Visibility == Visibility.Visible)
                    {
                        SaveFPS_Visibility = Visibility.Hidden;
                    }
                    SaveCount = 0;

                    if (SaveRemainCount_Visibility == Visibility.Visible)
                    {
                        SaveRemainCount_Visibility = Visibility.Hidden;
                    }

                    try
                    {
                        
                    }
                    catch
                    {
                        
                    }
                }
            }
        }

        public void ImageConvertThreadDo()
        {
            while (!flagStopImageConvertThread)
            {
                if (ConvertBitmapQueue.Count > 0)
                {
                    try
                    {
                        lock (lockConvertObj)
                        {
                            displayNextTime = DateTime.Now;

                            ImageData imageData = ConvertBitmapQueue.Dequeue();

                            if (imageData == null)
                            {
                                continue;
                            }

                            ImageProvider.Image image = imageData.Image;

                            BitmapFactory.CreateBitmap(out m_bitmap, image.Width, image.Height, image.Color);
                            BitmapFactory.UpdateBitmap(m_bitmap, image.Buffer, image.Width, image.Height, image.Color);

                            DisplayImage = ToWpfBitmap(m_bitmap);
                            imageData.Bitmap = m_bitmap;

                            if (DisplayFPS_Visibility == Visibility.Hidden)
                            {
                                DisplayFPS_Visibility = Visibility.Visible;
                            }

                            DisplayCount++;

                            TimeSpan subtime = displayNextTime.Subtract(displayPrevTime);

                            if (subtime.Seconds >= 1 && subtime.Milliseconds >= 0)
                            {
                                DisplayFPS = DisplayCount;
                                DisplayCount = 0;
                                displayPrevTime = displayNextTime;
                            }

                            // 이미지 취득 모드일 때 저장 큐 추가, 메모리 사용량 90% 이하일 시
                            if (UseSaveMode && !IsMemoryUsageFull)
                            {
                                SaveBitmapQueue.Enqueue(imageData);
                            }

                            if (SaveBitmapQueue.Count == 1)
                            {
                                savePrevTime = DateTime.Now;
                            }
                        }
                    }
                    catch
                    {
                        
                    }
                }
                else if (ConvertBitmapQueue.Count == 0 && SaveBitmapQueue.Count == 0)
                {
                    if (DisplayFPS_Visibility == Visibility.Visible)
                    {
                        DisplayFPS_Visibility = Visibility.Hidden;
                    }

                    DisplayCount = 0;
                }
            }
        }

        private PerformanceCounter memory = new PerformanceCounter("Memory", "Available KBytes", null);

        private ulong totalMemory = new Microsoft.VisualBasic.Devices.ComputerInfo().TotalPhysicalMemory / 1024;

        public void MemoryCheck()
        {
            while (!flagStopMemoryCheckThread)
            {
                try
                {
                    lock (lockMemoryCheckObj)
                    {
                        double percent = Convert.ToDouble(memory.NextValue() / totalMemory) * 100;
                        MemoryUsage = Convert.ToInt64(100 - percent);

                        if (MemoryUsage < 50)
                        {
                            IsMemoryUsageFull = false;
                        }

                        if (MemoryUsage < 80)
                        {
                            MemoryCheckColor = System.Windows.Media.Brushes.LimeGreen;
                            WarnningMsgVsi = Visibility.Hidden;
                        }

                        if (MemoryUsage >= 80)
                        {
                            MemoryCheckColor = System.Windows.Media.Brushes.Orange;
                            WarnningMsgVsi = Visibility.Visible;
                            WarnningMsg = "메모리 사용량 80% 이상 차지하고 있습니다." + Environment.NewLine + "90% 이상 시 이미지 취득을 종료합니다.";
                        }

                        if (MemoryUsage >= 90)
                        {
                            MemoryCheckColor = System.Windows.Media.Brushes.Red;
                            WarnningMsgVsi = Visibility.Visible;
                            WarnningMsg = "이미지 취득을 종료합니다.";
                            IsMemoryUsageFull = true;
                        }

                        if (IsMemoryUsageFull)
                        {
                            WarnningMsgVsi = Visibility.Visible;
                            WarnningMsg = "이전 이미지들의 처리를 기다리고 있습니다.";
                        }

                        Thread.Sleep(500);
                    }
                }
                catch
                {

                }
            }
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
    }
}
