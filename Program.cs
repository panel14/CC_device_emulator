using CC_device_emulator;
using CC_device_emulator.Properties;
using System.Text.Json;
using YandexIoTCoreExample;

TimerObject tObj = new();

Console.WriteLine("Welcome to device emulator. Choice device:\n0 - kitchen_device;\n1 - bathroom_device");
int deviceNum = int.Parse(Console.ReadLine());

Console.WriteLine("Choice message send period (in min):");
int period = int.Parse(Console.ReadLine());

switch (deviceNum)
{
    case 0:
        tObj = new TimerObject()
        {
            DeviceType = DeviceType.KITCHEN_DEVICE,
            ClientId = Resources.ClientKitchenId,
            ClientPassword = Resources.ClientKitchenPassword,
            Topic = Resources.TopicKitchen
        };

        break;
    case 1:
        tObj = new TimerObject()
        {
            DeviceType = DeviceType.BATHROOM_DEVICE,
            ClientId = Resources.ClientBathroomId,
            ClientPassword = Resources.ClientBathroomPassword,
            Topic = Resources.TopicBathroom
        };
        Console.WriteLine("Not released yeat.");
        break;
    default:
        Console.WriteLine("Unknown device");
        break;      
}

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine(DateTime.Now + ": device up;");
Console.ForegroundColor = ConsoleColor.White;

Console.WriteLine($"{tObj.ClientId}; {tObj.ClientPassword}; {tObj.Topic};");

TimerCallback callback = new(ProcessDevice);
Timer t = new(callback, tObj, 0, 1000 * 60 * period);

Console.WriteLine("Press any button to stop and exit");
Console.ReadLine();


static string GetJsonString(DeviceType type)
{
    switch (type)
    {
        case DeviceType.KITCHEN_DEVICE:
            return JsonSerializer.Serialize(
                new
                {
                    event_datetime = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ss.sssZ"),
                    in_cooking = GetRandomInt(8),
                    in_washing = GetRandomInt(3)
                } 
            );
        case DeviceType.BATHROOM_DEVICE:
            return JsonSerializer.Serialize(
                new
                {
                    event_datetime = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ss.sssZ"),
                    in_shower = GetRandomInt(3),
                    in_sink = GetRandomInt(5)
                }
            );
        default:
            break;
    }
    return string.Empty;
}

static int GetRandomInt(int max)
{
    return new Random().Next(max);
}

static async void ProcessDevice(object obj)
{
    TimerObject timerObject = (TimerObject)obj;

    using YaClient devClient = new();

    await devClient.Start(timerObject.ClientId, timerObject.ClientPassword);

    string json = GetJsonString(timerObject.DeviceType);
    if (json == string.Empty)
    {
        devClient.Stop();
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("ERROR: Unknown device type: " + timerObject.DeviceType);
        Console.ForegroundColor = ConsoleColor.White;
        return;
    }
    Console.WriteLine(DateTime.Now + "; data to send: " + json);

    await devClient.Publish(timerObject.Topic, json, MQTTnet.Protocol.MqttQualityOfServiceLevel.AtMostOnce);
    devClient.Stop();
}