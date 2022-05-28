using System;
using System.Collections;
using System.Diagnostics;
using System.Threading;
using nanoFramework.Azure.Devices.Client;
using nanoFramework.Azure.Devices.Shared;
using nanoFramework.Networking;
using nanoFramework.Json;
using System.Device.Gpio;
using System.Security.Cryptography.X509Certificates;
using nanoFramework.M2Mqtt;
using Iot.Device.Buzzer;
using nanoFramework.Hardware.Esp32;

namespace star_wars
{
    public class Program
    {

        // Azure IoTHub settings
        const string _hubName = "ENTER IOT HUB NAME";
        const string _deviceId = "ENTER IOT DEVICE ID";
        const string _IotBrokerAddress = "ENTER IOT HUB URL";
        const string _SasKey = "ENTER IOT DEVICE PRIMARY KEY"; 
        
        private static GpioController s_GpioController;

        private static GpioPin blueLED;
        private const int blueLEDPin = 2;
        private static GpioPin redLED;
        private const int redLEDPin = 5;
        private static Buzzer buzzer;
        private const int buzzerPin = 21;

        private static bool isBlinking = false;

        private const int c = 261;
        private const int d = 294;
        private const int e = 329;
        private const int f = 349;
        private const int fS = 370;
        private const int g = 391;
        private const int gS = 415;
        private const int a = 440;
        private const int aS = 466;
        private const int b = 494;
        private const int cH = 523;
        private const int cSH = 554;
        private const int dH = 587;
        private const int dSH = 622;
        private const int eH = 659;
        private const int fH = 698;
        private const int fSH = 740;
        private const int gH = 784;
        private const int gSH = 830;
        private const int aH = 880;

        // frequencies for the tones we're going to use
        // used http://home.mit.bme.hu/~bako/tonecalc/tonecalc.htm to get these

        public static void Main()
        {
            Debug.WriteLine("Hello from nanoFramework!");

            if (!ConnectToWifi()) return;

            DeviceClient azureIoT = new DeviceClient(_IotBrokerAddress, _deviceId, _SasKey, qosLevel: nanoFramework.M2Mqtt.Messages.MqttQoSLevel.AtMostOnce);
            

            try
            {

                s_GpioController = new GpioController();

                blueLED = s_GpioController.OpenPin(blueLEDPin, PinMode.Output);
                redLED = s_GpioController.OpenPin(redLEDPin, PinMode.Output);

                blueLED.Write(PinValue.Low);
                redLED.Write(PinValue.Low);

                Configuration.SetPinFunction(buzzerPin, DeviceFunction.PWM1);

                azureIoT.TwinUpdated += TwinUpdatedEvent;
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

            Thread blueLEDThread = new Thread(BlinkBlueLED);
            blueLEDThread.Start();
            Thread redLEDThread = new Thread(BlinkRedLED);
            redLEDThread.Start();

            Thread.Sleep(Timeout.InfiniteTimeSpan);

        }

        public static void beep(int frequencyInHertz, int timeInMilliseconds)
        {
            using (Buzzer buzzer = new Buzzer(buzzerPin))   // Initialize buzzer with software PWM connected to pin 21.
            {
                buzzer.PlayTone(frequencyInHertz, timeInMilliseconds); // Play tone with frequency 440 hertz for one second.
            }

            Thread.Sleep(20);
            // a little delay to make all notes sound separate
        }

        public static void startImperialMarch()
        {

            try
            {
                // for the sheet music see:
                // http://www.musicnotes.com/sheetmusic/mtd.asp?ppn=MN0016254
                // this is just a translation of said sheet music to frequencies / time in ms
                // used 500 ms for a quart note

                beep(a, 500);
                beep(a, 500);
                beep(a, 500);
                beep(f, 350);
                beep(cH, 150);

                beep(a, 500);
                beep(f, 350);
                beep(cH, 150);
                beep(a, 1000);
                // first bit

                beep(eH, 500);
                beep(eH, 500);
                beep(eH, 500);
                beep(fH, 350);
                beep(cH, 150);

                beep(gS, 500);
                beep(f, 350);
                beep(cH, 150);
                beep(a, 1000);
                //second bit...

                beep(aH, 500);
                beep(a, 350);
                beep(a, 150);
                beep(aH, 500);
                beep(gSH, 350);
                beep(gH, 125);

                beep(fSH, 125);
                beep(fH, 125);
                beep(fSH, 250);
                Thread.Sleep(250);
                beep(aS, 250);
                beep(dSH, 500);
                beep(dH, 350);
                beep(cSH, 125);
                // start of the interesting bit

                beep(cH, 125);
                beep(b, 125);
                beep(cH, 250);
                Thread.Sleep(250);
                beep(f, 250);
                beep(gS, 500);
                beep(f, 375);
                beep(a, 125);

                beep(cH, 500);
                beep(a, 375);
                beep(cH, 125);
                beep(eH, 1000);
                // more interesting stuff

                beep(aH, 500);
                beep(a, 350);
                beep(a, 150);
                beep(aH, 500);
                beep(gSH, 350);
                beep(gH, 125);

                beep(fSH, 125);
                beep(fH, 125);
                beep(fSH, 250);
                Thread.Sleep(250);
                beep(aS, 250);
                beep(dSH, 500);
                beep(dH, 350);
                beep(cSH, 125);
                // repeat... repeat

                beep(cH, 125);
                beep(b, 125);
                beep(cH, 250);
                Thread.Sleep(250);
                beep(f, 250);
                beep(gS, 500);
                beep(f, 375);
                beep(cH, 125);

                beep(a, 500);
                beep(f, 375);
                beep(cH, 125);
                beep(a, 1000);
                // and we're done \�/
            }
            catch (Exception ex)
            {

                Debug.WriteLine(ex.ToString());
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

            // Connect the ESP32 Device to the Wi-Fi and check the connection...
            Debug.WriteLine("Program Started, connecting to WiFi.");

            CancellationTokenSource cs = new(60000);            
            var success = WifiNetworkHelper.Reconnect(requiresDateTime: true, token: cs.Token);
            
            if (!success)
            {
                Debug.WriteLine($"Can't connect to wifi: {WifiNetworkHelper.Status}");
                if (WifiNetworkHelper.HelperException != null)
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

            if (state.ToLower() == "on")
            {
                isBlinking = true;
                Thread imperialMarchThread = new Thread(startImperialMarch);
                imperialMarchThread.Start();
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
