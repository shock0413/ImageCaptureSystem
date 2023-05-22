using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace H_ImageCapture_System
{
    public partial class MainEngine
    {

        public ICommand WhiteBalanceCmd { get { return (whiteBalance) ?? (whiteBalance = new DelegateCommand(SetWhiteBalance)); } }
        private ICommand whiteBalance;

    }
}
