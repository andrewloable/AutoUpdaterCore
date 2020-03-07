using System;
using System.Collections.Generic;
using System.Text;

namespace AutoUpdaterCore
{
    public class UpdateDownloadedEventArgs : EventArgs
    {
        public bool IsDownloaded { get; set; }
    }
}
