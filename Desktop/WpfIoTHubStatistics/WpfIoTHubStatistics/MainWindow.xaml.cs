using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
using Microsoft.Azure.Devices.Shared;
using System.Diagnostics;
using System.Windows.Threading;
using System.Runtime.Remoting.Channels.Ipc;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting;
using AzureIoTHubQuotaLibrary;
using System.Threading;

namespace WpfIoTHubStatistics
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
        }

        // for getting IoT Hub Quato
        string applicationId = "<< your application-id >>";
        string subscriptionId = "<< your subscription-id >>";
        string tenantId = "<< your tenant-id >>";
        string password = "<< your password >>";
        string resourceGroupName = "<< resource group name for IoT Hub >>";
        string iotHubName = "<< IoT Hub Name >>";

        string ownerConnectionString = "HostName=<< IoT Hub Name >>.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=<< Share Access Key for iothubowner >>";
        string deviceConnectionString = "HostName=<< IoT Hub Name >>.azure-devices.net;DeviceId=<< DeviceId >>;SharedAccessKey=<< Shared Access Key for DeviceId >>";
        string deviceId = "<< Device Id >>";

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            tbApplicationId.Text = applicationId;
            tbSubscriptionId.Text = subscriptionId;
            tbTenantId.Text = tenantId;
            tbPassword.Text = password;
            tbResourceGroupName.Text = resourceGroupName;
            tbResourceName.Text = iotHubName;

            tbOwnerCS.Text = ownerConnectionString;
            tbDeviceCS.Text = deviceConnectionString;
            tbDeviceId.Text = deviceId;

            InitializeIPC();
        }

        IpcServerChannel serverChannel;
        Mutex channelMutex;
        EventWaitHandle channelEWH;
        string channelName = "WpfIoTHubStatisticsChannel";
        string messageName = "InterActionStatistics";
        string mutexName = "WpfIoTHubStatisticsMutex";
        string ewhName = "WpfIoTHubStatisticsEWH";
        EGAzureIoTHubQuota.InterActionStatus interActionStatus = new EGAzureIoTHubQuota.InterActionStatus();

        private void InitializeIPC()
        {
            serverChannel = new IpcServerChannel(channelName);
            if(!Mutex.TryOpenExisting(mutexName,out channelMutex))
            {
                channelMutex = new Mutex(false, mutexName);
            }
            if(!EventWaitHandle.TryOpenExisting(ewhName,out channelEWH))
            {
                channelEWH = new EventWaitHandle(false, EventResetMode.AutoReset, ewhName);
            }
            ChannelServices.RegisterChannel(serverChannel,true);
            RemotingServices.Marshal(interActionStatus, messageName);

            new Thread(ReceiveInterMessage).Start();
        }

        private void ReceiveInterMessage()
        {
            while (true)
            {
                channelEWH.WaitOne();
                this.Dispatcher.Invoke(() => {
                    channelMutex.WaitOne();
                    SetActionStatistics(interActionStatus.Action, interActionStatus.DataSize);
                    channelMutex.ReleaseMutex();
                });

            }
        }

        QuotaClient quotaClient;
        IoTHubQuota lastQuota;
        ActionStatistics actionStatistics;
        private async void buttonRESTGet_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (quotaClient == null)
                {
                    quotaClient = new QuotaClient(
                        tbApplicationId.Text,
                        tbPassword.Text,
                        tbSubscriptionId.Text,
                        tbTenantId.Text,
                        tbResourceGroupName.Text,
                        tbResourceName.Text);
                }
                var quota = await quotaClient.GetQuotaAsync();
                var sb = new StringBuilder();
                if (actionStatistics != null)
                {
                    if (lastQuota != null)
                    {
                        actionStatistics.IncOfMessage = quota.TotalMessage - lastQuota.TotalMessage;
                        sb.AppendLine("Timestamp:" + actionStatistics.Timestamp.ToString("yyyy/MM/dd HH:mm:ss"));
                        sb.AppendLine("Action: " + actionStatistics.Action);
                        sb.AppendLine("Increased : " + actionStatistics.IncOfMessage);
                    }
                }
                else
                {
                    sb.AppendLine("Timestamp:" + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
                    sb.AppendLine(string.Format("CurrentMessages/Max : {0}/{1}", quota.TotalMessage, quota.MessageMaxValue));
                }
                lastQuota = quota;
                if (!string.IsNullOrEmpty(tbRESTResult.Text))
                {
                    sb.AppendLine("--------------------------------------------");
                    sb.Append(tbRESTResult.Text);
                }
                tbRESTResult.Text = sb.ToString();
            }
            catch(Exception ex)
            {
                new Task(() =>
                {
                    MessageBox.Show(ex.Message);
                }).Start();
            }
        }

        private void SetActionStatistics(string action, int dataSize)
        {
            actionStatistics = new ActionStatistics()
            {
                Timestamp = DateTime.Now
            };
            actionStatistics.Action = action;
            actionStatistics.DataSize = dataSize;
//            buttonRESTGet.IsEnabled = false;

            string msg = "Action:" + action + ",DataSize:" + dataSize + "@" + actionStatistics.Timestamp.ToString("yyyy/MM/dd-HH:mm:ss");

            var sb = new StringBuilder(msg);
            sb.AppendLine("-----------------------------------------------");
            sb.AppendLine(tbActionState.Text);
            tbActionState.Text = sb.ToString();

        }


        private byte[] createData(int size, bool random = false)
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
            char[] data = { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J' };
            var sb = new StringBuilder();
            int rest = size - minContent.Length;
            while (rest > 0)
            {
                if (random)
                {
                    sb.Append(data[rnd.Next(data.Length)]);
                }
                else
                {
                    sb.Append(data[rest-- % data.Length]);
                }
            }
            content = new
            {
                msg = sb.ToString()
            };
            return Encoding.UTF8.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(content));
        }

        private TestService testService;
        private async void buttonOpenC2D_Click(object sender, RoutedEventArgs e)
        {
            if (testService == null)
            {
                testService = new TestService(tbOwnerCS.Text, tbOwnerCS.Text);
            }
            try
            {
                await testService.OpenAsync();

                buttonSendC2D.IsEnabled = true;
                buttonReadDTDesiredS.IsEnabled = true;
                buttonReadDTReported.IsEnabled = true;
                buttonInvokeDMethod.IsEnabled = true;
                buttonWriteDTDesired.IsEnabled = true;
                buttonCloseC2D.IsEnabled = true;
                buttonOpenC2D.IsEnabled = false;

                SetActionStatistics("Open:Service", 0);
            }
            catch(Exception ex)
            {
                new Task(() => {
                    MessageBox.Show(ex.Message);
                }).Start();
            }
        }

        private async void buttonSendC2D_Click(object sender, RoutedEventArgs e)
        {
            int size = int.Parse(tbDataSizeC2D.Text);
            var content = createData(size);
            await testService.SendC2DMessage(tbDeviceId.Text, content);

            SetActionStatistics("SendC2D", size);
        }
        private async void buttonCloseC2D_Click(object sender, RoutedEventArgs e)
        {
            await testService.CloseAsync();

            buttonOpenC2D.IsEnabled = true;
            buttonSendC2D.IsEnabled = false;
            buttonReadDTDesiredS.IsEnabled = false;
            buttonWriteDTDesired.IsEnabled = false;
            buttonReadDTReported.IsEnabled = false;
            buttonInvokeDMethod.IsEnabled = false;
            buttonCloseC2D.IsEnabled = false;

            SetActionStatistics("Close", 0);
        }

        private async void buttonWriteDTDesired_Click(object sender, RoutedEventArgs e)
        {
            var dataSize = int.Parse(tbDTDesreidSizeS.Text);
            var content = createData(dataSize);
            await testService.UpdateDesiredProperties(tbDeviceId.Text, Encoding.UTF8.GetString(content));

            SetActionStatistics("WriteDTDesired", dataSize);
        }

        private async void buttonReadDTRepored_Click(object sender, RoutedEventArgs e)
        {
            var reporeted = await testService.ReadReportedProperties(tbDeviceId.Text);
            var size = Encoding.UTF8.GetBytes(reporeted).Length;
            tbDTReportedSizeS.Text = size.ToString();

            SetActionStatistics("ReadDTDesired:Service", size);
        }

        private async void buttonReadDTDesiredS_Click(object sender, RoutedEventArgs e)
        {
            var dtDesired = await testService.ReadDesiredProperties(tbDeviceId.Text);
            tbDTDesiredDataSizeS.Text = Encoding.UTF8.GetBytes(dtDesired).Length.ToString();

            SetActionStatistics("ReadDTDesired:Service", 0);
        }

        private async void buttonInvokeDMethod_Click(object sender, RoutedEventArgs e)
        {
            int payloadSize = int.Parse(tbMethodPayload.Text);
            await testService.InvokeDirectMethod(tbDeviceId.Text, tbDMethodName.Text, Encoding.UTF8.GetString(createData(payloadSize)));

            SetActionStatistics("InvokeDirectMethod", payloadSize);
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(e.Uri.ToString());
        }

        private void buttonLaunchTD_Click(object sender, RoutedEventArgs e)
        {
            string remoteChannelUri = "ipc://" + channelName + "/" + messageName;
            App.TestDevicePad = Process.Start("..\\..\\..\\TestDevicePad\\bin\\Debug\\TestDevicePad.exe", deviceConnectionString+" "+ remoteChannelUri+" "+ mutexName+" "+ewhName);
        }
    }
}
