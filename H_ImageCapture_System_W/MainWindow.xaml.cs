using CameraManager;
using H_ImageCapture_System.Struct;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace H_ImageCapture_System
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public bool ShutDown = false;

        MainEngine engine;

        public MainWindow()
        {
            // InitNotifyIcon();

            InitializeComponent();

            engine = new MainEngine(this);

            this.DataContext = engine;

            engine.OnMessageReceiveEvent += Engine_OnMessageReceiveEvent;
            // engine.UpdateDeviceList();
            engine.StartImageSaveThread();
            engine.RefreshCameraList();

            // engine.StartDisplayThread();
            // engine.StartImageSendThread();
            // engine.StartImageConvertThread();
            // engine.StartMemoryCheckThread();

            /*
            new Thread(() =>
            {
                while (isRunning)
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        bool isChecked = false;

                        if (engine.TabLiveViewVisibility == Visibility.Visible)
                        {
                            if (Display_CAM1.Image != null)
                            {
                                Display_CAM1.Image.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                                isChecked = true;
                            }
                        }

                        if (engine.SplitLiveViewVisibility == Visibility.Visible)
                        {
                            if (Split_Display_CAM1.Image != null)
                            {
                                Split_Display_CAM1.Image.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                                isChecked = true;
                            }
                        }

                        if (isChecked)
                        {
                            ms.Position = 0;
                            BitmapImage bi = new BitmapImage();
                            bi.BeginInit();
                            bi.StreamSource = ms;
                            bi.CacheOption = BitmapCacheOption.OnLoad;
                            bi.EndInit();

                            Mat mat = OpenCvSharp.Extensions.BitmapSourceConverter.ToMat(bi);
                            calcBlurriness(mat);
                        }
                    }

                    Thread.Sleep(10);
                }
            }).Start();
            */
        }

        private bool isRunning = true;

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

        private void Engine_OnMessageReceiveEvent(string message, DateTime dateTime, string type)
        {
            Dispatcher.Invoke(() =>
            {
                engine.MessageCollection.Add(new MessageData() { Time = dateTime, Message = message, Type = type });
                var border = VisualTreeHelper.GetChild(dg_Console, 0) as Decorator;
                if (border != null)
                {
                    var scroll = border.Child as ScrollViewer;
                    if (scroll != null) scroll.ScrollToEnd();
                }
            });
        }

        private void btn_OneShot_Click(object sender, RoutedEventArgs e)
        {
            LiveViewOnButton_Click(null, null);

            engine.OneShot();
        }

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            /*
            System.Windows.Controls.DataGrid gd = sender as System.Windows.Controls.DataGrid;
            HCamera cameraDevice = gd.SelectedItem as HCamera;

            if (cameraDevice != null)
            {
                cameraDevice.Close();
                cameraDevice.Open();
                
                if (cameraDevice.IsOpen)
                {
                    engine.SelectedCameraList.Add(cameraDevice);
                    engine.LoadParams();
                    engine.ShowConsole("카메라 접속 성공 : " + engine.SelectedCamera.Info.SerialNumber, DateTime.Now);
                }
                else
                {
                    engine.ShowConsole("카메라 접속 실패 : " + engine.SelectedCamera.Info.SerialNumber, DateTime.Now);
                }
                
            }
            */

            /*
            CameraDevice cameraDevice = gd.SelectedItem as CameraDevice;
            if (cameraDevice != null)
            {
                engine.Stop();

                engine.Close();

                engine.Open(cameraDevice);
            }
            */
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // e.Cancel = true;
            // this.WindowState = WindowState.Minimized;

            MessageBoxResult result= System.Windows.MessageBox.Show("정말 종료하시겠습니까?", "카메라 시스템 종료", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                engine.Stop();
                engine.Close();

                Environment.Exit(0);
            }
            else
            {
                e.Cancel = true;
            }

            /*
            MetroDialogSettings settings = new MetroDialogSettings();
            settings.AffirmativeButtonText = "네";
            settings.NegativeButtonText = "아니요";
            settings.OwnerCanCloseWithDialog = true;

            MessageDialogResult result = await this.ShowMessageAsync("카메라 시스템 종료", "정말 종료하시겠습니까?", MessageDialogStyle.AffirmativeAndNegative, settings: settings);

            if (result == MessageDialogResult.Affirmative)
            {
                engine.isRunning = false;
                ShutDown = true;
                engine.Stop();
                engine.Close();
                engine.StopImageSaveThread();
                // engine.StopDisplayThread();
                // engine.StopImageSendThread();
                // engine.StopImageConvertThread();
                // engine.StopMemoryCheckThread();

                Environment.Exit(0);
            }
            */
        }

        private void btn_Continuous_Click(object sender, RoutedEventArgs e)
        {
            LiveViewOnButton_Click(null, null);

            engine.ContinuousShot();
        }

        private void btn_StopContinuous_Click(object sender, RoutedEventArgs e)
        {
            engine.StopContinuousShot();
        }

        private void btn_ImageSavePath_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.SelectedPath = engine.ImageSavePath;
            if(dialog.ShowDialog()== System.Windows.Forms.DialogResult.OK)
            {
                engine.ImageSavePath = dialog.SelectedPath;
            }
        }

        private void btn_ConnectCamera_click(object sender, RoutedEventArgs e)
        {
            if (!isUnChecked)
            {
                if (!isChecked)
                {
                    isChecked = true;
                    HCamera cameraDevice = cameraCollectionDataGrid.SelectedItem as HCamera;
                    int index = cameraCollectionDataGrid.SelectedIndex;

                    if (cameraDevice != null)
                    {
                        cameraDevice.Close();
                        cameraDevice.Open();

                        IEnumerable<System.Windows.Controls.CheckBox> checkBoxList = cameraCollectionDataGrid.FindChildren<System.Windows.Controls.CheckBox>(true);

                        if (cameraDevice.IsOpen)
                        {
                            engine.SelectedCameraList.Add(cameraDevice);
                            engine.LoadParams();

                            engine.ShowConsole("카메라 접속 성공 : " + cameraDevice.Info.SerialNumber, DateTime.Now);
                            engine.IsSelectedCamera = true;

                            checkBoxList.ToList()[index].IsChecked = true;
                        }
                        else
                        {
                            engine.ShowConsole("카메라 접속 실패 : " + cameraDevice.Info.SerialNumber, DateTime.Now);

                            checkBoxList.ToList()[index].IsChecked = false;
                        }
                    }

                    isChecked = false;
                }
            }
        }

        private void btn_DisconnectCamera_click(object sender, RoutedEventArgs e)
        {
            if (!isChecked)
            {
                HCamera camera = cameraCollectionDataGrid.SelectedItem as HCamera;
                int index = cameraCollectionDataGrid.SelectedIndex;

                if (camera != null)
                {
                    isUnChecked = true;
                    string serialNumber = camera.Info.SerialNumber;
                    IEnumerable<System.Windows.Controls.CheckBox> checkBoxList = cameraCollectionDataGrid.FindChildren<System.Windows.Controls.CheckBox>(true);

                    if (camera.Close())
                    {
                        camera = null;

                        for (int i = 0; i < engine.SelectedCameraList.Count; i++)
                        {
                            HCamera selectedCamera = engine.SelectedCameraList[i];

                            if (selectedCamera.Info.SerialNumber == serialNumber)
                            {
                                engine.SelectedCameraList.RemoveAt(i);
                                break;
                            }
                        }

                        engine.ShowConsole("카메라 접속 해제 완료 : " + serialNumber, DateTime.Now);

                        checkBoxList.ToList()[index].IsChecked = false;

                        if (engine.SelectedCameraList.Count == 0)
                        {
                            engine.IsSelectedCamera = false;
                        }
                    }
                    else
                    {
                        engine.ShowConsole("카메라 접속 해제 실패 : " + serialNumber, DateTime.Now);

                        checkBoxList.ToList()[index].IsChecked = true;
                    }
                    isUnChecked = false;
                }
            }
        }

        private void InitNotifyIcon()
        {
            System.Windows.Forms.NotifyIcon notify = new System.Windows.Forms.NotifyIcon();
            notify.Icon = H_ImageCapture_System.Properties.Resources.camera;

            notify.Visible = true;

            System.Windows.Forms.ContextMenu menu = new System.Windows.Forms.ContextMenu();

            System.Windows.Forms.MenuItem itemClose = new System.Windows.Forms.MenuItem();
            itemClose.Text = "프로그램 종료";
            itemClose.Click += ItemClose_Click;

            menu.MenuItems.Add(itemClose);

            notify.ContextMenu = menu;
            notify.DoubleClick += Notify_DoubleClick;
        }

        private void Notify_DoubleClick(object sender, EventArgs e)
        {
            this.WindowState = WindowState.Minimized;
            this.WindowState = WindowState.Normal;
            this.Focus();

        }

        private async void ItemClose_Click(object sender, EventArgs e)
        {
            this.WindowState = WindowState.Normal;
            this.Focus();

            MetroDialogSettings settings = new MetroDialogSettings();
            settings.AffirmativeButtonText = "네";
            settings.NegativeButtonText = "아니요";
            settings.OwnerCanCloseWithDialog = true;
            MessageDialogResult result = await this.ShowMessageAsync("카메라 시스템 종료", "정말 종료하시겠습니까?", MessageDialogStyle.AffirmativeAndNegative, settings:settings);

            if(result == MessageDialogResult.Affirmative)
            {
                engine.isRunning = false;
                ShutDown = true;
                engine.Stop();
                engine.Close();
                engine.StopImageSaveThread();
                // engine.StopDisplayThread();
                // engine.StopImageSendThread();
                // engine.StopImageConvertThread();
                // engine.StopMemoryCheckThread();

                Environment.Exit(0);
            }
        }

        private void btn_SendEnd_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private System.Windows.Controls.DataGrid cameraCollectionDataGrid = null;

        private void CameraCollectionDataGrid_Loaded(object sender, RoutedEventArgs e)
        {
            cameraCollectionDataGrid = sender as System.Windows.Controls.DataGrid;
            cameraCollectionDataGrid.Items.Refresh();
        }

        private void IsOpenCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (!isUnChecked)
            {
                if (!isChecked)
                {
                    isChecked = true;
                    HCamera cameraDevice = cameraCollectionDataGrid.SelectedItem as HCamera;
                    int index = cameraCollectionDataGrid.SelectedIndex;

                    if (cameraDevice != null)
                    {
                        cameraDevice.Close();
                        cameraDevice.Open();
                        IEnumerable<System.Windows.Controls.CheckBox> checkBoxList = cameraCollectionDataGrid.FindChildren<System.Windows.Controls.CheckBox>(true);

                        if (cameraDevice.IsOpen)
                        {
                            engine.SelectedCameraList.Add(cameraDevice);
                            engine.LoadParams();

                            engine.ShowConsole("카메라 접속 성공 : " + cameraDevice.Info.SerialNumber, DateTime.Now);
                            engine.IsSelectedCamera = true;

                            checkBoxList.ToList()[index].IsChecked = true;
                        }
                        else
                        {
                            engine.ShowConsole("카메라 접속 실패 : " + cameraDevice.Info.SerialNumber, DateTime.Now);

                            checkBoxList.ToList()[index].IsChecked = false;
                        }
                    }

                    isChecked = false;
                }
            }
        }

        private bool isChecked = false;
        private bool isUnChecked = false;

        private void IsOpenCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!isChecked)
            {
                isUnChecked = true;

                HCamera camera = cameraCollectionDataGrid.SelectedItem as HCamera;
                int index = cameraCollectionDataGrid.SelectedIndex;

                if (camera != null)
                {
                    string serialNumber = camera.Info.SerialNumber;
                    IEnumerable<System.Windows.Controls.CheckBox> checkBoxList = cameraCollectionDataGrid.FindChildren<System.Windows.Controls.CheckBox>(true);

                    if (camera.Close())
                    {
                        camera = null;

                        for (int i = 0; i < engine.SelectedCameraList.Count; i++)
                        {
                            HCamera selectedCamera = engine.SelectedCameraList[i];

                            if (selectedCamera.Info.SerialNumber == serialNumber)
                            {
                                engine.SelectedCameraList.RemoveAt(i);
                                break;
                            }
                        }

                        engine.ShowConsole("카메라 접속 해제 완료 : " + serialNumber, DateTime.Now);
                        checkBoxList.ToList()[index].IsChecked = false;

                        if (engine.SelectedCameraList.Count == 0)
                        {
                            engine.IsSelectedCamera = false;
                        }
                    }
                    else
                    {
                        if (!isUnChecked)
                        {
                            engine.ShowConsole("카메라 접속 해제 실패 : " + serialNumber, DateTime.Now);
                            checkBoxList.ToList()[index].IsChecked = true;
                        }
                    }
                }

                isUnChecked = false;
            }
        }

        private void CameraCollectionDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            engine.SelectedCamera = cameraCollectionDataGrid.SelectedItem as HCamera;
        }

        public void LiveViewOnButton_Click(object sender, RoutedEventArgs e)
        {
            if (engine.SelectedCameraList.Count > 0)
            {
                for (int i = 0; i < engine.SelectedCameraList.Count; i++)
                {
                    HCamera camera = engine.SelectedCameraList[i];

                    if (!camera.IsGrabbing)
                    {
                        if (camera.Info.DeviceNumber == 1)
                        {
                            if (engine.TabLiveViewVisibility == Visibility.Visible)
                            {
                                camera.StartGrab(Display_CAM1.Handle);
                            }
                            else if (engine.SplitLiveViewVisibility == Visibility.Visible)
                            {
                                camera.StartGrab(Split_Display_CAM1.Handle);
                            }
                        }
                        else if (camera.Info.DeviceNumber == 2)
                        {
                            if (engine.TabLiveViewVisibility == Visibility.Visible)
                            {
                                camera.StartGrab(Display_CAM2.Handle);
                            }
                            else if (engine.SplitLiveViewVisibility == Visibility.Visible)
                            {
                                camera.StartGrab(Split_Display_CAM2.Handle);
                            }
                        }
                    }
                }
            }
        }

        public void LiveViewOffButton_Click(object sender, RoutedEventArgs e)
        {
            if (engine.SelectedCameraList.Count > 0)
            {
                for (int i = 0; i < engine.SelectedCameraList.Count; i++)
                {
                    HCamera camera = engine.SelectedCameraList[i];
                    camera.StopGrab();
                }
            }
        }

        public void btn_Refresh_click(object sender, RoutedEventArgs e)
        {
            engine.RefreshCameraList();
            cameraCollectionDataGrid.Items.Refresh();
        }

        private void IntegrateCameraLayoutButton_Click(object sender, RoutedEventArgs e)
        {
            engine.TabLiveViewVisibility = Visibility.Visible;
            engine.SplitLiveViewVisibility = Visibility.Hidden;

            if (engine.SelectedCameraList.Count > 0)
            {
                for (int i = 0; i < engine.SelectedCameraList.Count; i++)
                {
                    HCamera camera = engine.SelectedCameraList[i];

                    if (camera.IsGrabbing)
                    {
                        camera.StopGrab();

                        if (camera.Info.DeviceNumber == 1)
                        {
                            camera.StartGrab(Display_CAM1.Handle);
                        }
                        else if (camera.Info.DeviceNumber == 2)
                        {
                            camera.StartGrab(Display_CAM2.Handle);
                        }
                    }
                }
            }
        }

        private void SplitCameraLayoutButton_Click(object sender, RoutedEventArgs e)
        {
            engine.TabLiveViewVisibility = Visibility.Hidden;
            engine.SplitLiveViewVisibility = Visibility.Visible;

            if (engine.SelectedCameraList.Count > 0)
            {
                for (int i = 0; i < engine.SelectedCameraList.Count; i++)
                {
                    HCamera camera = engine.SelectedCameraList[i];

                    if (camera.IsGrabbing)
                    {
                        camera.StopGrab();

                        if (camera.Info.DeviceNumber == 1)
                        {
                            camera.StartGrab(Split_Display_CAM1.Handle);
                        }
                        else if (camera.Info.DeviceNumber == 2)
                        {
                            camera.StartGrab(Split_Display_CAM2.Handle);
                        }
                    }
                }
            }
        }
    }
}
