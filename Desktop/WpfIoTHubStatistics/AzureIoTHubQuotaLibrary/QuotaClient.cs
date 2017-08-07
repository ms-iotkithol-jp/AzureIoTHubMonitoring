using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace AzureIoTHubQuotaLibrary
{
    public class QuotaClient
    {
        ClientCredential credential;
        string applicationId;
        string subscriptionId;
        string tenantId;
        string resourceGroupName;
        string iotHubName;
        string password;

        bool isInitialized = false;

        public QuotaClient(string ApplicationId, string Password, string SubscriptionId, string TenantId, string ResourceGroupName, string IoTHubName)
        {
            applicationId = ApplicationId;
            subscriptionId = SubscriptionId;
            tenantId = TenantId;
            resourceGroupName = ResourceGroupName;
            iotHubName = IoTHubName;
            password = Password;
        }

        AuthenticationResult token;

        private async Task InitializeAsync()
        {
            try
            {
                string uri = string.Format("https://login.windows.net/{0}", tenantId);
                var context = new AuthenticationContext(uri);
                credential = new ClientCredential(applicationId, password);
                token = await context.AcquireTokenAsync("https://management.core.windows.net/", credential);
                if (token == null)
                {
                    throw new ArgumentOutOfRangeException("Invalid parameters");
                }
                isInitialized = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        readonly string apiVersion = "2017-01-19";
        public async Task<IoTHubQuota> GetQuotaAsync()
        {
            if (!isInitialized)
            {
                await InitializeAsync();
            }
            IoTHubQuota quota = null;
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);

                string mgmtUri = "management.azure.com";
                string restUri = string.Format($"https://{mgmtUri}/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Devices/IotHubs/{iotHubName}/quotaMetrics?api-version={apiVersion}");
                var response = await httpClient.GetAsync(restUri);
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    quota = new IoTHubQuota();
                    var jsonCotent = await response.Content.ReadAsStringAsync();
                    dynamic jsonRoot = Newtonsoft.Json.JsonConvert.DeserializeObject(jsonCotent);
                    dynamic valueToken = jsonRoot.SelectToken("value");
                    foreach (var value in valueToken)
                    {
                        dynamic nameToken = value.SelectToken("Name");
                        dynamic cvToken = value.SelectToken("CurrentValue");
                        dynamic mvToken = value.SelectToken("MaxValue");
                        string name = nameToken.Value;
                        long cv = cvToken.Value;
                        long mv = mvToken.Value;
                        switch (name)
                        {
                            case "TotalMessages":
                                quota.TotalMessage = cv;
                                quota.MessageMaxValue = mv;
                                break;
                            case "TotalDeviceCount":
                                quota.TotalDeviceCount = cv;
                                quota.TotalDeviceMaxValue = mv;
                                break;
                            default:
                                break;
                        }
                    }
                }
                return quota;
            }
        }
    }
}
