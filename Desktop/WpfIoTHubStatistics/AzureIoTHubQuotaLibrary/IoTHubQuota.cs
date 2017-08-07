using System;
using System.Collections.Generic;
using System.Text;

namespace AzureIoTHubQuotaLibrary
{
    public class IoTHubQuota
    {
        public long TotalMessage { get; set; }
        public long MessageMaxValue { get; set; }
        public long TotalDeviceCount { get; set; }
        public long TotalDeviceMaxValue { get; set; }
    }
}
