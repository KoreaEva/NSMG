using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Timers;
using Microsoft.Azure.Devices.Client;

namespace IoTHubDataSender
{
    class Program
    {
        //private static System.Timers.Timer SensorTimer;
        private const string DeviceConnectionString = "HostName=NSMGIothub.azure-devices.net;SharedAccessKeyName=device;SharedAccessKey=34N01FIfQTMF70HXaFZ66JnWUkBM4xrAckSFUy7zTFQ=";
        private const string DeviceID = "Device1";
        private static DummySensor Sensor = new DummySensor();
        private static int Duration = 0;
        private static int InstanceCount = 0;
        private string JsonFIlename = "";

        private static DeviceClient SensorDevice = null;

        static void Main(string[] args)
        {
            if(args.Length < 2)
            {
                Console.WriteLine("Please use parameta ex)IoTHubDataSender {Duration} {Instance count}");
                return;
            }

            if(!Int32.TryParse(args[0], out Duration))
            {
                Console.WriteLine("Incorret Duration type ex) 1000");
                return;
            }

            if (!Int32.TryParse(args[1], out InstanceCount))
            {
                Console.WriteLine("Incorret Instance Count type ex) 1000");
                return;
            }

            for(int i=0;i<InstanceCount;i++)
                SetTimer();

            SensorDevice = DeviceClient.CreateFromConnectionString(DeviceConnectionString, "Device1", TransportType.Mqtt_Tcp_Only);

            if (SensorDevice == null)
            {
                Console.WriteLine("Failed to create DeviceClient!");
                //SensorTimer.Stop();
            }

            Console.WriteLine("\nPress the Enter key to exit the application...\n");
            Console.WriteLine("The application started at {0:HH:mm:ss.fff}", DateTime.Now);
            Console.ReadLine();
            //SensorTimer.Stop();
            //SensorTimer.Dispose();
        }

        static async Task SendEvent()
        {
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(Sensor.GetWetherData(DeviceID));

            Console.WriteLine(json);

            Message eventMessage = new Message(Encoding.UTF8.GetBytes(json));
            await SensorDevice.SendEventAsync(eventMessage);
        }

        static async Task ReceiveCommands()
        {
            Message receivedMessage;
            string messageData;

            receivedMessage = await SensorDevice.ReceiveAsync(TimeSpan.FromSeconds(1));

            if (receivedMessage != null)
            {
                messageData = Encoding.ASCII.GetString(receivedMessage.GetBytes());
                Console.WriteLine("\t{0}> Received message: {1}", DateTime.Now.ToLocalTime(), messageData);

                await SensorDevice.CompleteAsync(receivedMessage);
            }
        }

        private static void SetTimer()
        {
            Timer SensorTimer = new Timer(Duration);
            SensorTimer.Elapsed += SensorTimer_Elapsed;
            SensorTimer.AutoReset = true;
            SensorTimer.Enabled = true;
        }

        private async static void SensorTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Console.WriteLine("The Elapsed event was raised at {0:HH:mm:ss.fff}", e.SignalTime);
            await SendEvent();
            await ReceiveCommands();
        }
    }
}
