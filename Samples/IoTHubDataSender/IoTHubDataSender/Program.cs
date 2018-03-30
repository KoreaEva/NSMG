using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Timers;
using Microsoft.Azure.EventHubs;

namespace IoTHubDataSender
{
    class Program
    {
        //private static System.Timers.Timer SensorTimer;
        private static DummySensor Sensor = new DummySensor();
        private static int Duration = 1000;

        private const string EventHubConnectionString = "Endpoint=sb://nsmgeventhub.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=uqC11Xtp5syZy0QF7jFGwDfJO4wgEaCxYyuX5Khrr0U=";
        private const string EventHub = "wifi";
        private static EventHubClient eventHubClient;

        static void Main(string[] args)
        {
            var connectionStringBuilder = new EventHubsConnectionStringBuilder(EventHubConnectionString)
            {
                EntityPath = EventHub
            };

            eventHubClient = EventHubClient.CreateFromConnectionString(connectionStringBuilder.ToString());

            SetTimer();

            if (eventHubClient == null)
            {
                Console.WriteLine("Failed to create EventHub Client!");
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
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(Sensor.GetWetherData("Device1"));

            await eventHubClient.SendAsync(new EventData(Encoding.UTF8.GetBytes(json)));
            Console.WriteLine(json);
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
        }
    }
}
