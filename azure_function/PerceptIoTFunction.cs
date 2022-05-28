using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.Devices;

namespace PerceptIoT.Function
{
    public static class PerceptIoTFunction
    {

        private static ServiceClient s_serviceClient;
        
        // Connection string for your IoT Hub
        // az iot hub show-connection-string --hub-name {your iot hub name} --policy-name service
        private static string s_connectionString = System.Environment.GetEnvironmentVariable("IotHubConnectionString");

        [FunctionName("PerceptIoTFunction")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            log.LogInformation(requestBody);

            dynamic data = JsonConvert.DeserializeObject(requestBody);

            log.LogInformation($"The State was: {data.state} ");
            log.LogInformation($"The Device was: {data.device}" );
            
            string responseMessage = $"This HTTP triggered function executed successfully. The State was {data.state}, the Device was {data.device}";

            s_serviceClient = ServiceClient.CreateFromConnectionString(s_connectionString);

            await InvokeMethodAsync(Convert.ToString(data.state), Convert.ToString(data.device));

            s_serviceClient.Dispose();

            return new OkObjectResult(responseMessage);
        }

         // Invoke the direct method on the device, passing the payload
        private static async Task InvokeMethodAsync(string state, string device)
        {
            var methodInvocation = new CloudToDeviceMethod("ControlLight")
            {
                ResponseTimeout = TimeSpan.FromSeconds(30),
            };

            methodInvocation.SetPayloadJson("{\"state\": \"" + state + "\"}");

            // Invoke the direct method asynchronously and get the response from the simulated device.
            var response = await s_serviceClient.InvokeDeviceMethodAsync(device, methodInvocation);

            Console.WriteLine($"\nResponse status: {response.Status}, payload:\n\t{response.GetPayloadAsJson()}");
        }
    }
}
