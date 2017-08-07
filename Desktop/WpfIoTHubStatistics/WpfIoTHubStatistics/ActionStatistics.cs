using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfIoTHubStatistics
{
    public class ActionStatistics
    {
        public DateTime Timestamp { get; set; }
        public string Action { get; set; }
        public int DataSize { get; set; }
        public long IncOfMessage { get; set; }
    }
}
