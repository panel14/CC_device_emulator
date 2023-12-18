using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using MQTTnet;
using MQTTnet.Client;

namespace YandexIoTCoreExample
{
    public enum EntityType
    {
        Registry = 0,
        Device = 1
    }

    public enum TopicType
    {
        Events = 0,
        Commands = 1
    }

    public class YaClient : IDisposable
    {
        private const string MqttServer = "mqtt.cloud.yandex.net";
        private const int MqttPort = 8883;

        private readonly static X509Certificate2 rootCrt = new (@"F:\Учеба\4 курс\Облака\4 этап\rootCA.crt");

        public delegate void OnSubscribedData(string topic, byte[] payload);
        public event OnSubscribedData SubscribedData;

        private IMqttClient mqttClient = null;
        private readonly ManualResetEvent oCloseEvent = new (false);
        private readonly ManualResetEvent oConnectedEvent = new (false);

        public async Task Start(string id, string password)
        {
            //setup connection options
            MqttClientTlsOptions tlsOptions = new ()
            {
                SslProtocol = SslProtocols.Tls12,
                UseTls = true,
                CertificateValidationHandler = CertificateValidationHandler
            };

            // Create TCP based options using the builder.
            var options = new MqttClientOptionsBuilder()
                .WithClientId(Guid.NewGuid().ToString())
                .WithTcpServer(MqttServer, MqttPort)
                .WithTlsOptions(tlsOptions)
                .WithCleanSession()
                .WithCredentials(id, password)
                .WithKeepAlivePeriod(TimeSpan.FromSeconds(900000))
                .Build();

            var factory = new MqttFactory();
            mqttClient = factory.CreateMqttClient();

            mqttClient.ApplicationMessageReceivedAsync += DataHandler;
            mqttClient.ConnectedAsync += ConnectedHandler;
            mqttClient.DisconnectedAsync += DisconnectedHandler;
            
            
            Console.WriteLine($"Connecting to mqtt.cloud.yandex.net...");

            await mqttClient.ConnectAsync(options);
        }

        public void Stop()
        {
            oCloseEvent.Set();
            mqttClient.DisconnectAsync();
        }

        public void Dispose()
        {
            Stop();
        }

        public Task Subscribe(string topic, MQTTnet.Protocol.MqttQualityOfServiceLevel qos)
        {
            return mqttClient.SubscribeAsync(topic, qos);
        }

        public Task Publish(string topic, string payload, MQTTnet.Protocol.MqttQualityOfServiceLevel qos)
        {
            var appMessage = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .WithQualityOfServiceLevel(qos)
                .Build();
            return mqttClient.PublishAsync(appMessage);
        }
        private Task ConnectedHandler(MqttClientConnectedEventArgs arg)
        {
            oConnectedEvent.Set();
            return Task.CompletedTask;
        }

        private static Task DisconnectedHandler(MqttClientDisconnectedEventArgs arg)
        {
            Console.WriteLine($"Disconnected mqtt.cloud.yandex.net.");
            return Task.CompletedTask;
        }

        private Task DataHandler(MqttApplicationMessageReceivedEventArgs arg)
        {
            SubscribedData(arg.ApplicationMessage.Topic, arg.ApplicationMessage.Payload);
            return Task.CompletedTask;
        }

        private static bool CertificateValidationHandler(MqttClientCertificateValidationEventArgs args)
        {
            try
            {
                if (args.SslPolicyErrors == SslPolicyErrors.None)
                    return true;

                if (args.SslPolicyErrors == SslPolicyErrors.RemoteCertificateChainErrors)
                {
                    args.Chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                    args.Chain.ChainPolicy.VerificationFlags = X509VerificationFlags.NoFlag;
                    args.Chain.ChainPolicy.ExtraStore.Add(rootCrt);

                    args.Chain.Build(rootCrt);
                    var res = args.Chain.ChainElements.Cast<X509ChainElement>().Any(a => a.Certificate.Thumbprint == rootCrt.Thumbprint);
                    return res;
                }
            }
            catch { }
            return false;
        }
    }
}