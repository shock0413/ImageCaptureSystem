using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CameraManager
{
    public class HGrabImage
    {
        public enum ShotType { OneShot, ContinousShot }
        public ShotType Type { get; set; }
        public string Name { get; set; }
        public Bitmap Bitmap { get; set; } 
    }
}
