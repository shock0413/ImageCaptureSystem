using PylonC.NET;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace H_Czech_Under_Body_Image_Acquisition_W
{
    /// <summary>
    /// App.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            try
            {
                Environment.SetEnvironmentVariable("PYLON_GIGE_HEARTBEAT", "10000" /*ms*/);

                PylonC.NET.Pylon.Initialize();
                try
                {
                    (new MainWindow()).ShowDialog();
                }
                catch
                {
                    PylonC.NET.Pylon.Terminate();
                    throw;
                }
                PylonC.NET.Pylon.Terminate();
            }catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
