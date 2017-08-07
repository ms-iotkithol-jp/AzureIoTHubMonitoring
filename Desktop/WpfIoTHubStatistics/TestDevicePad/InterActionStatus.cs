using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EGAzureIoTHubQuota
{
    public class InterActionStatus : MarshalByRefObject
    {
        public DateTime Timestamp { get; set; }
        public string Action { get; set; }
        public int DataSize { get; set; }

    }
}
