using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace H_ImageCapture_System
{
    /// <summary>
    /// App.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            Mutex mutex;
            string mutextName = "H_ImageCapture_System";
            bool createNew;
            mutex = new Mutex(true, mutextName, out createNew);
            if(createNew == false)
            {
                Shutdown(0);
            }
            else
            {
                // InitPylonSystem();
            }

            var mainWindow = new MainWindow();
            mainWindow.Show();
        }

        private void InitPylonSystem()
        {
            try
            {
                

                
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

       
    }
}
