using System;
using Microsoft.Azure.Devices.Client;
using System.Threading.Tasks;
using System.IO;
using System.Text;
using System.Device.Gpio;
using Newtonsoft.Json;

namespace device_messaging
{
    class Program
    {
        static string DeviceConnectionString = "ENTER YOUR DEVICE CONNECTION STRING";
        static int lightPin = 32;
          
        static async Task Main(string[] args)
        {

            GpioController controller = new GpioController(PinNumberingScheme.Board);
            
            controller.OpenPin(lightPin, PinMode.Output);
            controller.Write(lightPin, PinValue.High);
            
            DeviceClient Client = DeviceClient.CreateFromConnectionString(DeviceConnectionString, Microsoft.Azure.Devices.Client.TransportType.Mqtt);

            await Client.SetMethodHandlerAsync("ControlLight", (MethodRequest methodRequest, object userContext) => {

                Console.WriteLine("IoT Hub invoked the 'ControlLight' method.");
                Console.WriteLine("Payload:");
                Console.WriteLine(methodRequest.DataAsJson);

                dynamic data = JsonConvert.DeserializeObject(methodRequest.DataAsJson);

                if (data.state == "on")
                {
                    controller.Write(lightPin, PinValue.Low);
                }
                else
                {
                    controller.Write(lightPin, PinValue.High);
                }

                var responseMessage = "{\"response\": \"OK\"}";

                return Task.FromResult(new MethodResponse(Encoding.ASCII.GetBytes(responseMessage), 200));

            }, null);

            Console.WriteLine("Waiting for Command");

            while (true)
            {
                
            }

        }
    }
}
