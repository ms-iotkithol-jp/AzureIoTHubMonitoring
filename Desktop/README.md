# Desktop utility tool for Azure IoT Hub Quota mesurement 
## Preparation 
Please prepare several environmental string parameters by reading [Azure IoT Hub Resource Management REST API](https://docs.microsoft.com/azure/iot-hub/iot-hub-rm-rest). 
To run this utility you need following parameters for access Azure Resource Management. 
- application-id 
- subscription-id
- tenant-id 
- password 
In addition, you also need for Azure IoT Hub names and connection string. 
- resource group name
- IoT Hub name 
- connection string for iothubowner 
- connection string for your registed device id and connection string for your device id 

## Windows 
Open WpfIoTHubStatistics/WpfIoTHubStatistics.sln by Visual Studio 2017. 
Please edit above parameters in MainPage.xaml.cs 
Then run WpfIoTHubStatistics. 


## Non Windows 
You can run command line based utility tool written code on .NET Core. 
Please run NETCoreIoTHubStatistics by .NET Core tool. 
