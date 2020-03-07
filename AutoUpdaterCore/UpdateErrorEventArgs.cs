using System;
using System.Collections.Generic;
using System.Text;

namespace AutoUpdaterCore
{
    public class UpdateErrorEventArgs : EventArgs
    {
        public string Message { get; set; }
    }
}
