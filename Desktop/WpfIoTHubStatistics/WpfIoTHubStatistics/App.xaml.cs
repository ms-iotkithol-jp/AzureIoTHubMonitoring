using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace WpfIoTHubStatistics
{
    /// <summary>
    /// App.xaml の相互作用ロジック
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            this.Startup += App_Startup;
            this.Exit += App_Exit;
        }

        private void App_Startup(object sender, StartupEventArgs e)
        {
        }

        private void App_Exit(object sender, ExitEventArgs e)
        {
            if (TestDevicePad != null)
            {
                TestDevicePad.Kill();
                
            }
        }

        static public Process TestDevicePad { get; set; }
    }
}
