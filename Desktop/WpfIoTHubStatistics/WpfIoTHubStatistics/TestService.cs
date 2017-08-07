using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;

namespace WpfIoTHubStatistics
{
    public class TestService
    {
        private string serviceConnectionString;
        private string ownerConnectionString;

        private Microsoft.Azure.Devices.ServiceClient serviceClient;
        private Microsoft.Azure.Devices.RegistryManager registryManager;

        public TestService(string serviceCS, string ownerCS)
        {
            serviceConnectionString = serviceCS;
            ownerConnectionString = ownerCS;
        }

        public async Task OpenAsync()
        {
            serviceClient = Microsoft.Azure.Devices.ServiceClient.CreateFromConnectionString(serviceConnectionString);
            
            await serviceClient.OpenAsync();

            registryManager = Microsoft.Azure.Devices.RegistryManager.CreateFromConnectionString(ownerConnectionString);
            await registryManager.OpenAsync();
        }

        public async Task CloseAsync()
        {
            await serviceClient.CloseAsync();
            await registryManager.CloseAsync();
        }
        public async Task SendC2DMessage( string deviceId,byte[] msg)
        {
            var message = new Microsoft.Azure.Devices.Message(msg);
            await serviceClient.SendAsync(deviceId, message);
        }

        public async Task UpdateDesiredProperties(string deviceId, string jsonTwinPatch)
        {
            var twin = await registryManager.GetTwinAsync(deviceId);
            await registryManager.UpdateTwinAsync(deviceId, jsonTwinPatch, twin.ETag);
        }

        public async Task<string> ReadDesiredProperties(string deviceId)
        {
            var twin = await registryManager.GetTwinAsync(deviceId);
            var  desired = twin.Properties.Desired.ToJson();
            Debug.WriteLine("Read - desired properties : size="+Encoding.UTF8.GetBytes(desired).Length);
            return desired;
        }

        public async Task<string> ReadReportedProperties(string deviceId)
        {
            var twin = await registryManager.GetTwinAsync(deviceId);
            var reported = twin.Properties.Reported.ToJson();
            Debug.WriteLine("Read - reported properties : size=" + Encoding.UTF8.GetBytes(reported).Length);
            return reported;
        }

        public async Task InvokeDirectMethod(string deviceId, string methodName, string payload)
        {
            var c2dMethod = new CloudToDeviceMethod(methodName);
            c2dMethod.SetPayloadJson(payload);
            var response = await serviceClient.InvokeDeviceMethodAsync(deviceId, c2dMethod);
        }
    }
}
