# Percept Home Automation

An Azure Function and Device Code to allow for connection between the Azure Percept Audio and an IoT Device.

# Instructions

- Clone the repo
- Create an Shared Access Policy in the Percept IoT Hub with "Service Connect" Permission
- Take a copy of the Connection String
- Create a new Device in the IoT hub 
- Take a copy of the Connection String
- Edit azure_function\local.settings.json and paste in the IoT Hub Percept IoT Hub Service Connect Connection String
- Edit device_code\Program.cs and paste in the IoT Device Connection String
- Deploy the "azure_function" Function App 
- Run the "device_code" app with `dotnet run`

For the full instructions, please follow this post;

http://bit.ly/percept-home-automation