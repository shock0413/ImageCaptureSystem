using PylonC.NETSupportLibrary;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace H_Czech_Under_Body_Image_Acquisition_W.Struct
{
    public class ImageData
    {
        public ImageProvider.Image Image { get; set; }

        public Bitmap Bitmap { get { return bitmap; } set { bitmap = value; } }
        private Bitmap bitmap;

        public DateTime DateTime { get; set; }
        public string Name { get; set; }
    }
}
