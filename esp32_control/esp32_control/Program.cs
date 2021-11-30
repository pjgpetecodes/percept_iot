using System;
using System.Collections;
using System.Diagnostics;
using System.Threading;
using nanoFramework.Azure.Devices.Client;
using nanoFramework.Azure.Devices.Shared;
using nanoFramework.Networking;
using nanoFramework.Json;
using System.Device.Gpio;

namespace esp32_control
{
    public class Program
    {

        private static string _ssid = "ENTER WIFI SSDI";
        private static string _wifiPassword = "ENTER WIFI PASSWORD";

        // Azure IoTHub settings
        const string _hubName = "ENTER IOT HUB NAME";
        const string _deviceId = "ENTER IOT DEVICE ID";
        const string _IotBrokerAddress = "ENTER IOT HUB URL";
        const string _SasKey = "ENTER IOT DEVICE PRIMARY KEY";

        private static GpioController s_GpioController;

        private static GpioPin whiteLED;
        private static GpioPin blueLED;
        private static GpioPin greenLED;
        private static GpioPin yellowLED;
        private static GpioPin redLED;

        private static bool isBlinking = false;

        public static void Main()
        {
            Debug.WriteLine("Hello from nanoFramework!");

            DeviceClient azureIoT = new DeviceClient(_IotBrokerAddress, _deviceId, _SasKey);

            try
            {

                s_GpioController = new GpioController();

                whiteLED = s_GpioController.OpenPin(1, PinMode.Output);
                blueLED = s_GpioController.OpenPin(2, PinMode.Output);
                greenLED = s_GpioController.OpenPin(3, PinMode.Output);
                yellowLED = s_GpioController.OpenPin(4, PinMode.Output);
                redLED = s_GpioController.OpenPin(5, PinMode.Output);

                whiteLED.Write(PinValue.Low);
                blueLED.Write(PinValue.Low);
                greenLED.Write(PinValue.Low);
                yellowLED.Write(PinValue.Low);
                redLED.Write(PinValue.Low);

                if (!ConnectToWifi()) return;

                azureIoT.TwinUpated += TwinUpdatedEvent;
                azureIoT.StatusUpdated += StatusUpdatedEvent;
                azureIoT.CloudToDeviceMessage += CloudToDeviceMessageEvent;
                azureIoT.AddMethodCallback(ControlLight);

                var isOpen = azureIoT.Open();
                Debug.WriteLine($"Connection is open: {isOpen}");

                var twin = azureIoT.GetTwin(new CancellationTokenSource(20000).Token);
                if (twin == null)
                {
                    Debug.WriteLine($"Can't get the twins");
                    azureIoT.Close();
                    return;
                }

                Debug.WriteLine($"Twin DeviceID: {twin.DeviceId}, #desired: {twin.Properties.Desired.Count}, #reported: {twin.Properties.Reported.Count}");

                TwinCollection reported = new TwinCollection();
                reported.Add("firmware", "myNano");
                reported.Add("sdk", 0.2);
                azureIoT.UpdateReportedProperties(reported);



            }
            catch (Exception ex)
            {
                // We won't do anything
                // This global try catch is to make sure whatever happen, we will safely be able to
                // reboot or do anything else.
                Debug.WriteLine(ex.ToString());
            }

            Thread whiteLEDThread = new Thread(BlinkWhiteLED);
            whiteLEDThread.Start();
            Thread blueLEDThread = new Thread(BlinkBlueLED);
            blueLEDThread.Start();
            Thread greenLEDThread = new Thread(BlinkGreenLED);
            greenLEDThread.Start();
            Thread yellowLEDThread = new Thread(BlinkYellowLED);
            yellowLEDThread.Start();
            Thread redLEDThread = new Thread(BlinkRedLED);
            redLEDThread.Start();

            Thread.Sleep(Timeout.InfiniteTimeSpan);

        }

        public static void BlinkWhiteLED()
        {
            while (true)
            {
                if (isBlinking)
                {
                    var randomGenerator = new Random();
                    var randomDelay = randomGenerator.Next(1000);

                    whiteLED.Toggle();
                    Thread.Sleep(randomDelay);
                }
                else
                {
                    whiteLED.Write(PinValue.Low);
                }
            }
        }

        public static void BlinkBlueLED()
        {
            while (true)
            {
                if (isBlinking)
                {
                    var randomGenerator = new Random();
                    var randomDelay = randomGenerator.Next(1000);

                    blueLED.Toggle();
                    Thread.Sleep(randomDelay);
                }
                else
                {
                    blueLED.Write(PinValue.Low);
                }
            }
        }

        public static void BlinkGreenLED()
        {
            while (true)
            {
                if (isBlinking)
                {
                    var randomGenerator = new Random();
                    var randomDelay = randomGenerator.Next(1000);

                    greenLED.Toggle();
                    Thread.Sleep(randomDelay);
                }
                else
                {
                    greenLED.Write(PinValue.Low);
                }
            }
        }

        public static void BlinkYellowLED()
        {
            while (true)
            {
                if (isBlinking)
                {
                    var randomGenerator = new Random();
                    var randomDelay = randomGenerator.Next(1000);

                    yellowLED.Toggle();
                    Thread.Sleep(randomDelay);
                }
                else
                {
                    yellowLED.Write(PinValue.Low);
                }
            }
        }
        public static void BlinkRedLED()
        {
            while (true)
            {
                if (isBlinking)
                {
                    var randomGenerator = new Random();
                    var randomDelay = randomGenerator.Next(1000);

                    redLED.Toggle();
                    Thread.Sleep(randomDelay);
                }
                else
                {
                    redLED.Write(PinValue.Low);
                }
            }
        }

        public static bool ConnectToWifi()
        {
            Debug.WriteLine("Program Started, connecting to WiFi.");

            // As we are using TLS, we need a valid date & time
            // We will wait maximum 1 minute to get connected and have a valid date
            var success = NetworkHelper.ConnectWifiDhcp(_ssid, _wifiPassword, setDateTime: true, token: new CancellationTokenSource(60000).Token);
            if (!success)
            {
                Debug.WriteLine($"Can't connect to wifi: {NetworkHelper.ConnectionError.Error}");
                if (NetworkHelper.ConnectionError.Exception != null)
                {
                    Debug.WriteLine($"NetworkHelper.ConnectionError.Exception");
                }
            }

            Debug.WriteLine($"Date and time is now {DateTime.UtcNow}");
            return success;
        }

        public static void TwinUpdatedEvent(object sender, TwinUpdateEventArgs e)
        {
            Debug.WriteLine($"Twin update received:  {e.Twin.Count}");
        }

        public static void StatusUpdatedEvent(object sender, StatusUpdatedEventArgs e)
        {
            Debug.WriteLine($"Status changed: {e.IoTHubStatus.Status}, {e.IoTHubStatus.Message}");
            // You may want to reconnect or use a similar retry mechanism
            if (e.IoTHubStatus.Status == Status.Disconnected)
            {
                Debug.WriteLine("Stoppped!!!");
            }
        }

        public static string ControlLight(int rid, string payload)
        {
            Debug.WriteLine($"Call back called :-) rid={rid}, payload={payload}");

            Hashtable variables = (Hashtable)JsonConvert.DeserializeObject(payload, typeof(Hashtable));
            string state = (string)variables["state"];

            Debug.WriteLine("State: " + state);

            if (state == "on")
            {
                isBlinking = true;
            }
            else
            {
                isBlinking = false;
            }

            return "{\"response\":\"OK\"}";
        }

       
        public static void CloudToDeviceMessageEvent(object sender, CloudToDeviceMessageEventArgs e)
        {
            Debug.WriteLine($"Message arrived: {e.Message}");
            foreach (string key in e.Properties.Keys)
            {
                Debug.Write($"  Key: {key} = ");
                if (e.Properties[key] == null)
                {
                    Debug.WriteLine("null");
                }
                else
                {
                    Debug.WriteLine((string)e.Properties[key]);
                }
            }

        }

    }
}
