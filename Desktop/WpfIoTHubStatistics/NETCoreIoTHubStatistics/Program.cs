using AzureIoTHubQuotaLibrary;
using System;
using System.Threading.Tasks;

namespace NETCoreIoTHubStatistics
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("dotnet NETCoreIoTHubStatistics applicationId, subscriptionId, tenantId, password resourceGroupName iotHubName");
                Console.WriteLine("Please see https://docs.microsoft.com/ja-jp/rest/api/");
                return;
            }

            var p = new Program(args);
            p.Run().Wait();
        }

        string applicationId = "[application-id]";
        string subscriptionId = "[subscription-id]";
        string tenantId = "[tenant-id]";
        string password = "[password]";
        string resourceGroupName = "[resource-group-name]";
        string iotHubName = "[iot-hub-name]";

        public Program(string[] args)
        {
            applicationId = args[0];
            subscriptionId = args[1];
            tenantId = args[2];
            password = args[3];
            resourceGroupName = args[4];
            iotHubName = args[5];
        }

        public async Task Run()
        {
            var quotaClient = new QuotaClient(applicationId, password, subscriptionId, tenantId, resourceGroupName, iotHubName);
            try
            {
                var result = await quotaClient.GetQuotaAsync();
                Console.WriteLine("MeasuredTime:" + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
                Console.WriteLine("TotalMessage:" + result.TotalMessage + "/MaxMessage:" + result.MessageMaxValue + ",TotalDevice:" + result.TotalDeviceCount + "/MaxDevice:" + result.TotalDeviceMaxValue);
            }
            catch(Exception ex)
            {
                Console.WriteLine("Please check arguments or network!");
                Console.WriteLine(ex.Message);
            }
        }
    }
}