using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace TestDevicePad
{
    /// <summary>
    /// App.xaml の相互作用ロジック
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            this.Startup += App_Startup;
        }

        private void App_Startup(object sender, StartupEventArgs e)
        {
            TempStatus = e.Args;

            if (e.Args.Length == 4)
            {
                DeviceConnectionString = e.Args[0];
                remoteChannelUri = e.Args[1];
                mutexName = e.Args[2];
                ewhName = e.Args[3];
            }

        }

        public static string[] TempStatus { get; set; }
        public static string DeviceConnectionString { get; set; }

        public static string remoteChannelUri { get; set; }
        public static string mutexName { get; set; }
        public static string ewhName { get; set; }

    }
}
