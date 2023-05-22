using H_ImageCapture_System.Struct;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using CameraManager;

namespace H_ImageCapture_System
{
    public partial class MainEngine : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }

        public string MainIP { get { return configIni.GetString("Server", "IP", "127.0.0.1"); } set { configIni.WriteValue("Server", "IP", value); NotifyPropertyChanged("MainIP"); } }

        public int MainPort { get { return configIni.GetInt32("Server", "Port", 8080); ; } set { configIni.WriteValue("Server", "Port", value); NotifyPropertyChanged("MainPort"); } }

        public string EngineIP { get { return configIni.GetString("Server", "EngineIP", "127.0.0.1"); } set { configIni.WriteValue("Server", "EngineIP", value); NotifyPropertyChanged("EngineIP"); } }

        public int EnginePort { get { return configIni.GetInt32("Server", "EnginePort", 8080); ; } set { configIni.WriteValue("Server", "Port", value); NotifyPropertyChanged("EnginePort"); } }

        /*
        public ObservableCollection<CameraDevice> CameraCollection { get { return cameraCollection; } set { cameraCollection = value; NotifyPropertyChanged("CameraCollection"); } }
        public ObservableCollection<CameraDevice> cameraCollection;
        */

        public ObservableCollection<HCamera> CameraCollection { get { return cameraCollection; } set { cameraCollection = value; NotifyPropertyChanged("CameraCollection"); } }
        private ObservableCollection<HCamera> cameraCollection = new ObservableCollection<HCamera>();

        public ObservableCollection<MessageData> MessageCollection { get { return messageCollection; } set { messageCollection = value; NotifyPropertyChanged("MessageCollection"); } }
        public ObservableCollection<MessageData> messageCollection;

        public BitmapSource DisplayImage1 { get { return displayImage1; } set { displayImage1 = value; NotifyPropertyChanged("DisplayImage1"); } }
        private BitmapSource displayImage1;

        public BitmapSource DisplayImage2 { get { return displayImage2; } set { displayImage2 = value; NotifyPropertyChanged("DisplayImage2"); } }
        private BitmapSource displayImage2;

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

        public bool IsSelectedCamera { get { return isSelectedCamera; } set { isSelectedCamera = value; NotifyPropertyChanged("IsSelectedCamera"); } }
        private bool isSelectedCamera;

        public int Gain { get { return gain; } set { gain = value; NotifyPropertyChanged("Gain"); } }
        private int gain = 0;

        public int GainMin { get { return gainMin; } set { gainMin = value; NotifyPropertyChanged("GainMin"); } }
        private int gainMin = 0;

        public int GainMax { get { return gainMax; } set { gainMax = value; NotifyPropertyChanged("GainMax"); } }
        private int gainMax;

        public int DigitalGain { get { return digitalGain; } set { digitalGain = value; NotifyPropertyChanged("DigitalGain"); } }
        private int digitalGain = 0;

        public int DigitalGainMin { get { return digitalGainMin; } set { digitalGainMin = value; NotifyPropertyChanged("DigitalGainMin"); } }
        private int digitalGainMin = 0;

        public int DigitalGainMax { get { return digitalGainMax; } set { digitalGainMax = value; NotifyPropertyChanged("DigitalGainMax"); } }
        private int digitalGainMax;

        public int Exposure {
            get
            {
                return exposure;
            }
            set
            {
                exposure = value;
                NotifyPropertyChanged("Exposure");

                if (selectedCamera != null)
                {
                    selectedCamera.Parameters.Exposure = Convert.ToUInt32(exposure);
                }
            }
        }
        private int exposure = 16;

        public int ExposureMin { get { return exposureMin; } set { exposureMin = value; NotifyPropertyChanged("ExposureMin"); } }
        private int exposureMin;

        public int ExposureMax { get { return exposureMax; } set { exposureMax = value; NotifyPropertyChanged("ExposureMax"); } }
        private int exposureMax;

        public int ExposureInterval
        {
            get
            {
                return exposureInterval;
            }
            set
            {
                exposureInterval = value;
                NotifyPropertyChanged("ExposureInterval");

                if (selectedCamera != null)
                {
                    exposureInterval = Convert.ToInt32(selectedCamera.Parameters.ExposureInterval);
                }
            }
        }
        private int exposureInterval;

        public int FrameRate
        {
            get
            {
                return configIni.GetInt32("Info", "FrameRate", 60);
            }
            set
            {
                configIni.WriteValue("Info", "FrameRate", value);

                NotifyPropertyChanged("FrameRate");
                NotifyPropertyChanged("CaptureSpeed");

                logManager.Info("캡쳐 속도 : " + CaptureSpeed);
            }
        }

        public int FrameRateMin { get { return frameRateMin; } set { frameRateMin = value; NotifyPropertyChanged("FrameRateMin"); } }
        private int frameRateMin = 1;

        public int FrameRateMax { get { return frameRateMax; } set { frameRateMax = value; NotifyPropertyChanged("FrameRateMax"); } }
        private int frameRateMax = 120;

        public int PacketSize { get { return packetSize; } set { packetSize = value; NotifyPropertyChanged("PacketSize"); } }
        private int packetSize;

        public Visibility PacketSizeVisi { get { return packetSizeVisi; } set { packetSizeVisi = value; NotifyPropertyChanged("PacketSizeVisi"); } }
        private Visibility packetSizeVisi;

        public long InterPacketDelay { get { return interPacketDelay; } set { interPacketDelay = value; NotifyPropertyChanged("InterPacketDelay"); } }
        private long interPacketDelay;

        public Visibility InterPacketVisi { get { return interPacketVisi; } set { interPacketVisi = value; NotifyPropertyChanged("InterPacketVisi"); } }
        private Visibility interPacketVisi;

        public Visibility SensorReadoutModeVisi { get { return sensorReadoutModeVisi; } set { sensorReadoutModeVisi = value; NotifyPropertyChanged("SensorReadoutModeVisi"); } }
        private Visibility sensorReadoutModeVisi;

        public bool IsSensorReadoutNormal { get { return isSensorReadoutNormal; } set { if (isContinousShot) { return; } isSensorReadoutNormal = value; NotifyPropertyChanged("IsSensorReadoutNormal"); } }
        private bool isSensorReadoutNormal;

        public bool IsSensorReadoutFast { get { return isSensorReadoutFast; } set { if (isContinousShot) { return; } isSensorReadoutFast = value; NotifyPropertyChanged("IsSensorReadoutFast"); } }
        private bool isSensorReadoutFast;

        public bool EnableFrameRate {
            get
            {
                return configIni.GetBoolean("Info", "EnableFrameRate", false);
            }
            set
            {
                configIni.WriteValue("Info", "EnableFrameRate", value);
                NotifyPropertyChanged("EnableFrameRate");
            }
        }

        public bool EnablePacketSize { get { return enablePacketSize; } set { enablePacketSize = value; NotifyPropertyChanged("EnablePacketSize"); } }
        private bool enablePacketSize;

        public string ImageSavePath
        {
            get
            {
                return configIni.GetString("Info", "ImageSavePath", "C:\\Result");
            }
            set
            {
                configIni.WriteValue("Info", "ImageSavePath", value);
                NotifyPropertyChanged("ImageSavePath");
            }
        }


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


        public int CaptureLimit { get { return captureLimit; } set { captureLimit = value; NotifyPropertyChanged("CaptureLimit"); } }
        private int captureLimit = 1;

        public bool UseCaptureLimit { get { return useCaptureLimit; } set { useCaptureLimit = value; NotifyPropertyChanged("UseCaptureLimit"); } }
        private bool useCaptureLimit;

        public bool UseSaveMode
        {
            get
            {
                return configIni.GetBoolean("Info", "UseSaveMode", false);
            }
            set
            {
                configIni.WriteValue("Info", "UseSaveMode", value);
                NotifyPropertyChanged("UseSaveMode");
            }
        }

        private int interval = 0;
        public int Interval
        {
            get
            {
                return interval;
            }
            set
            {
                interval = value;
                NotifyPropertyChanged("Interval");
            }
        }

        public long MemoryUsage { get { return memoryUsage; } set { memoryUsage = value; NotifyPropertyChanged("MemoryUsage"); } }
        private long memoryUsage = 0;

        public System.Windows.Media.Brush MemoryCheckColor { get { return memoryCheckColor; } set { memoryCheckColor = value; NotifyPropertyChanged("MemoryCheckColor"); } }
        private System.Windows.Media.Brush memoryCheckColor;

        public Visibility WarnningMsgVsi { get { return warnningMsgVsi; } set { warnningMsgVsi = value; NotifyPropertyChanged("WarnningMsgVsi"); } }
        private Visibility warnningMsgVsi = Visibility.Hidden;

        public string WarnningMsg { get { return warnningMsg; } set { warnningMsg = value; NotifyPropertyChanged("WarnningMsg"); } }
        private string warnningMsg;

        public ObservableCollection<string> SendWaitImages
        {
            get
            {
                return new ObservableCollection<string>(SendBitmapPathQueue);//SendBitmapPathQueue 
                //return null;
            }
        }

        private bool isUseFocusAssist = false;
        public bool IsUseFocusAssist 
        {
            get { return isUseFocusAssist; }
            set
            {
                isUseFocusAssist = value;

                if (isUseFocusAssist)
                {
                    WarnningMsgVsi = Visibility.Visible;
                }
                else
                {
                    WarnningMsgVsi = Visibility.Hidden;
                }

                NotifyPropertyChanged("IsUseFocusAssist");
            }
        }

        private Visibility tabLiveViewVisibility = Visibility.Visible;
        public Visibility TabLiveViewVisibility
        {
            get
            {
                return tabLiveViewVisibility;
            }
            set
            {
                tabLiveViewVisibility = value;
                NotifyPropertyChanged("TabLiveViewVisibility");
            }
        }

        private Visibility splitLiveViewVisibility = Visibility.Hidden;
        public Visibility SplitLiveViewVisibility
        {
            get
            {
                return splitLiveViewVisibility;
            }
            set
            {
                splitLiveViewVisibility = value;
                NotifyPropertyChanged("SplitLiveViewVisibility");
            }
        }
    }
}
