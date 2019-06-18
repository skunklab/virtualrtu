using IoTEdge.VirtualRtu.Configuration;
using Piraeus.Clients.Mqtt;
using SkunkLab.Channels;
using SkunkLab.Channels.WebSocket;
using SkunkLab.Protocols.Mqtt;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace IoTEdge.VirtualRtu.FieldGateway.Communications
{
    public class CommunicationDirector
    {
        private static CommunicationDirector instance;
        private static Random ran;


        public static CommunicationDirector Create(EdgeGatewayConfiguration config)
        {
            ran = new Random();
            if(instance == null)
            {
                instance = new CommunicationDirector(config);
            }

            return instance;
        }

        protected CommunicationDirector(EdgeGatewayConfiguration config)
        {
            this.config = config;
            clientId = Guid.NewGuid().ToString();
            CreateWebSocketClient();
            CreateHttpClient();
        }

        private EdgeGatewayConfiguration config;
        private IChannel channel;
        private CancellationTokenSource cts;
        private string clientId;
        private PiraeusMqttClient pclient;
        private HttpClient httpClient;
        private string requestUrl;
        private int delay;

        public async Task SendRtuOutputAsync(byte[] message)
        {
            await pclient.PublishAsync(QualityOfServiceLevelType.AtMostOnce, config.RtuOutputPiSsytem, Constants.CONTENT_TYPE, message);
        }


        private void CreateWebSocketClient()
        {
            try
            {
                Uri uri = new Uri(String.Format($"wss://{config.Hostname}/ws/api/connect"));
                cts = new CancellationTokenSource();
                channel = new WebSocketClientChannel(uri, "mqtt", new WebSocketConfig(), cts.Token);
                channel.OnClose += Channel_OnClose;
                pclient = new PiraeusMqttClient(new MqttConfig(180), channel);

                ConnectAckCode code = pclient.ConnectAsync(clientId, "JWT", config.SecurityToken, 90).GetAwaiter().GetResult();
                Console.WriteLine($"MQTT client connection code = {code}");
                if (code != ConnectAckCode.ConnectionAccepted)
                {
                    throw new Exception("MQTT connection failed.");
                }

                pclient.OnChannelError += Pclient_OnChannelError;               
                pclient.SubscribeAsync(config.RtuInputPiSystem, QualityOfServiceLevelType.AtMostOnce, RtuInput).GetAwaiter();
                Console.WriteLine($"Field gateway subscribed to {config.RtuInputPiSystem}");
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Exception creating web socket client - {ex.Message}");
                try
                {
                    pclient.CloseAsync().GetAwaiter();
                }
                catch { }

                SetDelay();
                Task.Delay(delay).GetAwaiter();
                CreateWebSocketClient();
            }
        }

        private void Channel_OnClose(object sender, ChannelCloseEventArgs e)
        {
            Console.WriteLine("Field gateway channel closing.");
            //channel was closed
            SetDelay();
            //Task.Delay(delay).GetAwaiter();
            Task task = Task.Delay(delay);
            task.Wait();
            CreateWebSocketClient();
        }

        private void Pclient_OnChannelError(object sender, ChannelErrorEventArgs args)
        {
            cts.Cancel();
            Console.WriteLine($"Web socket channel error - {args.Error.Message}");
        }

        private void SetDelay()
        {
            if (delay == 0)
            {
                delay = Convert.ToInt32(Math.Round(15000.0 * ran.NextDouble(), 0));
            }
            else
            {
                delay = delay * 2;
            }

            if (delay >= 160000)
            {
                delay = delay = Convert.ToInt32(Math.Round(15000.0 * ran.NextDouble(), 0)); ;
            }
        }

        private void CreateHttpClient()
        {
            httpClient = new HttpClient();
            requestUrl = IPHelper.GetLocalAdapterRequestUrl(config.ModBusContainer, config.ModBusPort, config.ModBusPath);
        }

        private void RtuInput(string topic, string contentType, byte[] message)
        {
            //forward to ModBus Protocol Adpater
            ForwardToModBusProtocolAdapterAsync(message).GetAwaiter();
        }

        private async Task ForwardToModBusProtocolAdapterAsync(byte[] message)
        {
            try
            {
                HttpContent content = new System.Net.Http.ByteArrayContent(message);
                content.Headers.ContentType = new MediaTypeHeaderValue(Constants.CONTENT_TYPE);
                content.Headers.ContentLength = message.Length;
                string requestUrl = IPHelper.GetLocalAdapterRequestUrl(config.ModBusContainer, config.ModBusPort, config.ModBusPath);
                HttpResponseMessage response = await httpClient.PostAsync(requestUrl, content);
                Console.WriteLine("{0} - Forwarded msg to PA with status code '{1}'", DateTime.Now.ToString("hh:MM:ss.ffff"), response.StatusCode);
            }
            catch (WebException we)
            {
                Console.WriteLine("Web exception - {0}", we.Message);
                if (we.InnerException != null)
                {
                    Console.WriteLine("Web inner exception - {0}", we.InnerException.Message);

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Post exception - {0}", ex.Message);
                if (ex.InnerException != null)
                {
                    Console.WriteLine("Post inner exception - {0}", ex.InnerException.Message);
                }
            }
        }

        

        
    }
}
