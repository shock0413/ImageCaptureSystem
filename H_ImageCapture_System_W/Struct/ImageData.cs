using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace H_ImageCapture_System.Struct
{
    public class ImageData
    {

        private BitmapSource bitmapImage;
        public BitmapSource BitmapImage
        {
            get
            {
                return bitmapImage;
            }
            set
            {
                bitmapImage = value;
            }
        }

        public Bitmap Bitmap { get { return bitmap; } set { bitmap = value; } }
        private Bitmap bitmap;

        public DateTime DateTime { get; set; }
        public string Name { get; set; }
        public string Dir { get; set; }
    }
}
