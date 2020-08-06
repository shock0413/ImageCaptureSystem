using PylonC.NETSupportLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace H_Czech_Under_Body_Image_Acquisition_W.Struct
{
    public class CameraDevice
    {
        public DeviceEnumerator.Device Device { get { return device; } set { device = value; } }
        private DeviceEnumerator.Device device;

        public string FullName { get { return device.FullName; } }
        public uint Index { get { return device.Index; }}
        public string Name { get { return device.Name; }}
        public string ToolTip { get { return device.Tooltip; }}
        public string SerialNum { get { return device.SerialNum; } }

        public CameraDevice(DeviceEnumerator.Device device)
        {
            this.Device = device;
        }
    }
}
