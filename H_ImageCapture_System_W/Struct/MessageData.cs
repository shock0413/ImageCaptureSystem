using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace H_ImageCapture_System.Struct
{
    public class MessageData
    {
        public DateTime Time { get; set; }
        public string TimeStr { get { return Time.ToString("yyyy-MM-dd HH:mm:ss fff"); } }

        public string Message { get; set; }
        public string Type { get; set; }
    }
}
