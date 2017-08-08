using Microsoft.Azure.Devices.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TestDevicePad
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        //  for IoT Hub Client
        string deviceConnectionString = "";

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(App.DeviceConnectionString))
            {
                tbDeviceCS.Text = App.DeviceConnectionString;
            }
            else
            {
                tbDeviceCS.Text = deviceConnectionString;
            }
            remoteChannelUri = App.remoteChannelUri;
            mutexName = App.mutexName;
            ewhName = App.ewhName;
        }

        IpcClientChannel clientChannle;
        EGAzureIoTHubQuota.InterActionStatus interActionStatus;
        Mutex channelMutex;
        EventWaitHandle channelEWH;

        string remoteChannelUri;
        string mutexName;
        string ewhName;

        private void InitializeInterMessage()
        {
            if (clientChannle == null)
            {
                clientChannle = new IpcClientChannel();
                ChannelServices.RegisterChannel(clientChannle, true);
                interActionStatus = Activator.GetObject(typeof(EGAzureIoTHubQuota.InterActionStatus), remoteChannelUri) as EGAzureIoTHubQuota.InterActionStatus;
                channelMutex = Mutex.OpenExisting(mutexName);
                channelEWH = EventWaitHandle.OpenExisting(ewhName);
            }
        }

        private void SetActionStatus(string action, int dataSize)
        {
            channelMutex.WaitOne();
            interActionStatus.Action = action;
            interActionStatus.DataSize = dataSize;
            interActionStatus.Timestamp = DateTime.Now;
            channelMutex.ReleaseMutex();
            channelEWH.Set();
        }

        TestDevice testDevice = null;
        Brush defaultBackground;

        private async void buttonOpenD2C_Click(object sender, RoutedEventArgs e)
        {
            InitializeInterMessage();

            testDevice = new TestDevice(tbDeviceCS.Text);
            testDevice.C2DMessageReceived += TestDevice_C2DMessageReceived;
            testDevice.DesiredPropertiesUpdate += TestDevice_DesiredPropertiesUpdate;
            testDevice.InvokeDirectedMethod += TestDevice_InvokeDirectedMethod;
            try
            {
                await testDevice.ConnectAsync(cbDTCallback.IsChecked.Value);

                defaultBackground = this.Background;
                                this.Background = new SolidColorBrush(Colors.Azure);

                buttonCloseD2C.IsEnabled = true;
                buttonSendD2C.IsEnabled = true;
                buttonWriteDTReported.IsEnabled = true;
                buttonReadDTDesired.IsEnabled = true;
                buttonReadDTReportedD.IsEnabled = true;
                buttonOpenD2C.IsEnabled = false;

                SetActionStatistics("Open:Device", 0);
            }
            catch (Exception ex)
            {
                new Task(() =>
                {
                    MessageBox.Show(ex.Message);
                }).Start();
            }
        }

        private Microsoft.Azure.Devices.Client.Message currentReceivedMessage;
        private void TestDevice_C2DMessageReceived(object sender, Microsoft.Azure.Devices.Client.Message receivedMessage)
        {
            buttonSendConfirmation.IsEnabled = true;
            currentReceivedMessage = receivedMessage;
        }
        private void TestDevice_DesiredPropertiesUpdate(object sender, TwinCollection dp)
        {
            string json = dp.ToJson();
            tbDTDesiredSize.Text = Encoding.UTF8.GetBytes(json).Length.ToString();
        }
        private void TestDevice_InvokeDirectedMethod(object sender, string payload)
        {
            tbReceivedDMPayloadSize.Text = Encoding.UTF8.GetBytes(payload).Length.ToString();
        }

        private async void buttonCloseD2C_Click(object sender, RoutedEventArgs e)
        {
            if (testDevice == null) return;
            await testDevice.CloseAsync();
            this.Background = defaultBackground;
            
            buttonOpenD2C.IsEnabled = true;
            buttonSendD2C.IsEnabled = false;
            buttonReadDTDesired.IsEnabled = false;
            buttonWriteDTReported.IsEnabled = false;
            buttonReadDTReportedD.IsEnabled = false;
            buttonCloseD2C.IsEnabled = false;

            SetActionStatistics("Close:Device", 0);
        }

        private async void buttonSendD2C_Click(object sender, RoutedEventArgs e)
        {
            if (testDevice == null) return;
            int size = int.Parse(tbDataSizeD2C.Text);
            int psize = int.Parse(tbSendPropertySize.Text);
            if (psize>0)
            {
                await testDevice.SendAsync(createData(size),"p",createSizedSring(psize));
            }
            else
            {
                await testDevice.SendAsync(createData(size));
            }

            SetActionStatistics("SendD2C", size);
        }
        private async void buttonSendConfirmation_Click(object sender, RoutedEventArgs e)
        {
            await testDevice.ComfirmReceivedMessageAsync(currentReceivedMessage);
            currentReceivedMessage = null;
            buttonSendConfirmation.IsEnabled = false;

            SetActionStatistics("SendC2DConfirmation:Device", 0);
        }

        private async void buttonReadDTDesired_Click(object sender, RoutedEventArgs e)
        {
            if (testDevice == null) return;
            var dpJson = await testDevice.ReadDesiredProperties();
            tbDTDesiredSize.Text = Encoding.UTF8.GetBytes(dpJson).Length.ToString();

            SetActionStatistics("ReadDTDesired:Device", 0);
        }

        private async void buttonWriteDTReported_Click(object sender, RoutedEventArgs e)
        {
            if (testDevice == null) return;
            int size = int.Parse(tbDTReportedSize.Text);
            await testDevice.UpdateReportedProperties(Encoding.UTF8.GetString(createData(size)));

            SetActionStatistics("WriteDTReported", size);
        }
        private async void buttonReadDTReported_Click(object sender, RoutedEventArgs e)
        {
            if (testDevice == null)
            {
                return;
            }
            var rp = await testDevice.ReadReporetedProperties();
            var size = Encoding.UTF8.GetBytes(rp).Length;
            tbReadDTReportedD.Text = size.ToString();

            SetActionStatistics("ReadDTReporetedOnDevice", size);
        }

        private void SetActionStatistics(string action, int dataSize)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Last Action:");
            sb.AppendLine("Action:" + action);
            sb.AppendLine("DataSize:" + dataSize);
            sb.AppendLine("Timestamp:" + DateTime.Now.ToString("yyyy/MM/dd-HH:mm:ss"));
            tbAction.Text = sb.ToString();

            SetActionStatus(action, dataSize);
        }

        char[] creationDataUnits = { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J' };

        private string createSizedSring(int size)
        {
            var sb = new StringBuilder();
            for(int i = 0; i < size; i++)
            {
                sb.Append(creationDataUnits[i % creationDataUnits.Length]);
            }
            return sb.ToString();
        }
        private byte[] createData(int size,  bool random = false)
        {
            var rnd = new Random(DateTime.Now.Millisecond);
            var content = new
            {
                msg = ""
            };
            var minContent = Encoding.UTF8.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(content));
            if (size <= minContent.Length)
            {
                return minContent;
            }
            var sb = new StringBuilder();
            int rest = size - minContent.Length;
            while (rest > 0)
            {
                if (random)
                {
                    sb.Append(creationDataUnits[rnd.Next(creationDataUnits.Length)]);
                }
                else
                {
                    sb.Append(creationDataUnits[rest-- % creationDataUnits.Length]);
                }
            }
            content = new
            {
                msg = sb.ToString()
            };
            return Encoding.UTF8.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(content));
        }

        private void buttonSetDMethodReturnSize_Click(object sender, RoutedEventArgs e)
        {
            testDevice.MethodResponseDataSize = int.Parse(tbDMethodReturnSize.Text);
        }
    }
}
