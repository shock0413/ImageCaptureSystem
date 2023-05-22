using PylonC.NETSupportLibrary;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace H_Czech_Under_Body_Image_Acquisition
{
    public partial class Main_Form : Form
    {
        private Cameras m_Cameras;

        public Main_Form()
        {
            InitializeComponent();

            InitCamera();
        }

        private void InitCamera()
        {
            m_Cameras = new Cameras();
             
            bool initCameraChecker = false;
            m_Cameras.Init_Camera(1);
        }


        private void button1_Click(object sender, EventArgs e)
        {
            NewImageCapture();
        }
    }
}
