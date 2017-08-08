using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestDevicePad
{
    public class TestDevice
    {
        string connectionString;

        public TestDevice(string cs)
        {
            connectionString = cs;
        }

        Microsoft.Azure.Devices.Client.DeviceClient deviceClient;

        public async Task ConnectAsync(bool isDesiredPropertiesCallbackEnable, TransportType protocol = TransportType.Mqtt)
        {
            deviceClient = Microsoft.Azure.Devices.Client.DeviceClient.CreateFromConnectionString(connectionString, protocol);
            if (isDesiredPropertiesCallbackEnable && (protocol == TransportType.Mqtt))
            {
                await deviceClient.SetDesiredPropertyUpdateCallbackAsync(DesiredPropertyUpdate, this);
            }
            if (protocol == TransportType.Mqtt)
            {
                await deviceClient.SetMethodDefaultHandlerAsync(MethodHandler, this);
            }
            await deviceClient.OpenAsync();
            ReceiveMessages();
        }

        private Microsoft.Azure.Devices.Client.Message receivedMessage;
        private async Task ReceiveMessages()
        {
            while (true)
            {
                var msg = await deviceClient.ReceiveAsync();
                if (msg != null)
                {
                    lock (this)
                    {
                        receivedMessage = msg;
                        if (C2DMessageReceived != null)
                        {
                            C2DMessageReceived(this, receivedMessage);
                        }
                    }
                }
            }
        }

        int methodResponseDataSize;
        public int MethodResponseDataSize
        {
            get { return methodResponseDataSize; }
            set
            {
                methodResponseDataSize = value;
                var msgOrigin = new { msg = "" };
                var msgJson = Newtonsoft.Json.JsonConvert.SerializeObject(msgOrigin);
                int restSize = methodResponseDataSize - Encoding.UTF8.GetBytes(msgJson).Length;

                methodResponseData = new byte[restSize];
                var sb = new StringBuilder();
                char[] units = { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J' };
                for (int i = 0; i < restSize; i++)
                {
                    sb.Append(units[i % units.Length]);
                }
                msgOrigin = new { msg = sb.ToString() };
                methodResponseData = Encoding.UTF8.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(msgOrigin));
            }
        }
        private byte[] methodResponseData = { };
        private Task<MethodResponse> MethodHandler(MethodRequest methodRequest, object userContext)
        {
            if (InvokeDirectedMethod != null)
            {
                InvokeDirectedMethod(methodRequest, methodRequest.DataAsJson);
            }
            return new Task<MethodResponse>(() => {
                var responce = new MethodResponse(methodResponseData, 0);
                return responce;
            });
        }

        public async Task SendAsync(byte[] message,string propKey=null, string propValue=null)
        {
            var msg = new Microsoft.Azure.Devices.Client.Message(message);
            if (!string.IsNullOrEmpty(propKey))
            {
                msg.Properties.Add(propKey, propValue);
            }
            await deviceClient.SendEventAsync(msg);

        }

        public async Task ComfirmReceivedMessageAsync(Microsoft.Azure.Devices.Client.Message message)
        {
            await deviceClient.CompleteAsync(message);
        }

        public async Task<string> ReadDesiredProperties()
        {
            var twin = await deviceClient.GetTwinAsync();
            var dp = twin.Properties.Desired.ToJson();
            Debug.WriteLine("Received Desired Properties - size = " + Encoding.UTF8.GetBytes(dp).Length);
            Debug.WriteLine(dp);
            return dp;
        }

        public async Task<string> ReadReporetedProperties()
        {
            var twin = await deviceClient.GetTwinAsync();
            var rp = twin.Properties.Reported.ToJson();
            Debug.WriteLine("Read Reporeted Properties - size = " + Encoding.UTF8.GetBytes(rp).Length);
            Debug.WriteLine(rp);
            return rp;
        }

        public async Task UpdateReportedProperties(string reporetedJson)
        {
            var twin = await deviceClient.GetTwinAsync();
            var reporeted = Newtonsoft.Json.JsonConvert.DeserializeObject<TwinCollection>(reporetedJson);
            await deviceClient.UpdateReportedPropertiesAsync(reporeted);
        }

        public async Task CloseAsync()
        {
            await deviceClient.CloseAsync();
        }
        private Task DesiredPropertyUpdate(TwinCollection desiredProperties, object userContext)
        {
            int size = Encoding.UTF8.GetBytes(desiredProperties.ToJson()).Length;
            return new Task(() =>
            {
                Debug.WriteLine("Received Desired Property - size=" + size);
                Debug.WriteLine(desiredProperties.ToJson());
                if (DesiredPropertiesUpdate != null)
                {
                    DesiredPropertiesUpdate(this, desiredProperties);
                }
            });
        }

        public delegate void C2DMessageReceivedEvent(object sender, Microsoft.Azure.Devices.Client.Message receivedMessage);
        public event C2DMessageReceivedEvent C2DMessageReceived;

        public delegate void DesiredPropertiesUpdateEvent(object sender, TwinCollection dp);
        public event DesiredPropertiesUpdateEvent DesiredPropertiesUpdate;

        public delegate void InvokedDirectMethodEvent(object sender, string payload);
        public event InvokedDirectMethodEvent InvokeDirectedMethod;
    }
}
