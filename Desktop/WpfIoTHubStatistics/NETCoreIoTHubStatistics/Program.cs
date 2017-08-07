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

        string applicationId = "568526a0-e624-4f57-828b-e605b062c624";
        string subscriptionId = "d685a1cf-9bbd-4a90-8321-ac54287fb087";
        string tenantId = "72f988bf-86f1-41af-91ab-2d7cd011db47";
        string password = "P@ssw0rd.1";
        string resourceGroupName = "eg20170712";
        string iotHubName = "egiothub20170712";

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