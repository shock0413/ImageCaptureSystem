using H_Czech_Under_Body_Image_Acquisition_W.Struct;
using MahApps.Metro.Controls;
using PylonC.NETSupportLibrary;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace H_Czech_Under_Body_Image_Acquisition_W
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public bool ShutDown = false;

        MainEngine engine = new MainEngine();

        public MainWindow()
        {
            InitializeComponent();

            this.DataContext = engine;

            engine.OnMessageReceiveEvent += Engine_OnMessageReceiveEvent;
            engine.UpdateDeviceList();
            engine.StartImageSaveThread();
            engine.StartImageConvertThread();
            engine.StartMemoryCheckThread();
        }

        private void Engine_OnMessageReceiveEvent(string message, DateTime dateTime, string type)
        {
            engine.MessageCollection.Insert(0, new MessageData() { Time = dateTime, Message = message, Type = type });
        }

        private void btn_OneShot_Click(object sender, RoutedEventArgs e)
        {
            engine.OneShot();
        }

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            System.Windows.Controls.DataGrid gd = sender as System.Windows.Controls.DataGrid;
            CameraDevice cameraDevice = gd.SelectedItem as CameraDevice;
            if (cameraDevice != null)
            {
                engine.Stop();

                engine.Close();

                engine.Open(cameraDevice);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ShutDown = true;
            engine.Stop();
            engine.Close();
            engine.StopImageSaveThread();
            engine.StopImageConvertThread();
            engine.StopMemoryCheckThread();
        }

        private void btn_Continuous_Click(object sender, RoutedEventArgs e)
        {
            /*
            engine.StartImageSaveThread();
            engine.StartImageConvertThread();
            */
            engine.ContinuousShot();
        }

        private void btn_StopContinuous_Click(object sender, RoutedEventArgs e)
        {
            engine.StopContinuousShot();
            
            /*
            engine.StopImageSaveThread();
            engine.StopImageConvertThread();
            */
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

        private void btn_DisconnectCamera_click(object sender, RoutedEventArgs e)
        {
            if(engine.CurrentCamera!= null)
            {
                engine.Stop();
                engine.Close();
            }
        }
    }
}
