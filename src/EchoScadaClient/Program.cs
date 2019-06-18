
using IoTEdge.VirtualRtu.Configuration;
using Microsoft.IdentityModel.JsonWebTokens;
using Newtonsoft.Json;
using SkunkLab.Channels;

using System;
using System.IO;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading;

namespace EchoScadaClient
{
    class Program
    {
        static void Main(string[] args)
        {

            //string jwtString = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2t1bmtsYWIuaW8vbmFtZSI6ImRldmljZTciLCJodHRwOi8vc2t1bmtsYWIuaW8vcm9sZSI6ImRlbW92cnR1IiwibmJmIjoxNTYwMTg2OTQ3LCJleHAiOjE1OTE3MjI5NDcsImlhdCI6MTU2MDE4Njk0NywiaXNzIjoiaHR0cDovL3NrdW5rbGFiLmlvLyIsImF1ZCI6Imh0dHA6Ly9za3Vua2xhYi5pby8ifQ.JgvXZfn36XQ9CQPzt_7DNLVlJCX-_g6zmG3wc6TFHWk";
            //string jwtString = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2t1bmtsYWIuaW8vbmFtZSI6InZydHUiLCJodHRwOi8vc2t1bmtsYWIuaW8vcm9sZSI6Im1hbmFnZSIsIm5iZiI6MTU2MDE4ODI1NywiZXhwIjoxNTkxNzI0MjU3LCJpYXQiOjE1NjAxODgyNTcsImlzcyI6Imh0dHA6Ly9za3Vua2xhYi5pby8iLCJhdWQiOiJodHRwOi8vc2t1bmtsYWIuaW8vIn0.Vkx3MLxymnlf_xlFN0l8Yt_PoqwtsugnEtRmmqOApDE";
            //string jwtString = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2t1bmtsYWIuaW8vbmFtZSI6ImRlbW92cnR1LWRldmljZTEiLCJuYmYiOjE1NjAyNTQ1OTcsImV4cCI6MTU5MTc5MDU5NywiaWF0IjoxNTYwMjU0NTk3LCJpc3MiOiJodHRwOi8vc2t1bmtsYWIuaW8vIiwiYXVkIjoiaHR0cDovL3NrdW5rbGFiLmlvLyJ9.mlYROEu9jlq_bAs7M894al8hcM_9ebGd4Cmy9Tfi1ws";
            string jwtString = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2t1bmtsYWIuaW8vbmFtZSI6ImRlbW92cnR1LWRldmljZTEiLCJuYmYiOjE1NjAyNjM0MDcsImV4cCI6MTU5MTc5OTQwNywiaWF0IjoxNTYwMjYzNDA3LCJpc3MiOiJodHRwOi8vc2t1bmtsYWIuaW8vIiwiYXVkIjoiaHR0cDovL3NrdW5rbGFiLmlvLyJ9.K_RtV9d7tZ1qSbyy4PHZVmGcC4Uq3D4EjrL7BWY3Sg0";
            JsonWebToken jwt = new JsonWebToken(jwtString);
            //Match match = new Match(LiteralMatchExpression.MatchUri, "http://skunklab.io/name", true);
            //EvaluationOperation op = new EvaluationOperation(EqualOperation.OperationUri, "device7");
            //Rule rule = new Rule(match, op, true);


            //AuthorizationPolicy policy = new AuthorizationPolicy(rule, new Uri("http://www.skunklab.io/policy/demovrtu/unitid7-in"));

            //ClaimsIdentity identity = new ClaimsIdentity(jwt.Claims);
            //bool result = policy.Evaluate(identity);



            Console.WriteLine("----Test SCADA Echo Client-----");
            Console.WriteLine("press any key to continue");
            Console.ReadKey();

            //Console.WriteLine("Enter VRTU IP address or hostname ? ");

            //string publicIP = "40.121.83.251";
            //string publicIP = "172.18.144.1";
            string publicIP = GetIPAddressString(System.Net.Dns.GetHostName());
            //string publicIP = "40.114.9.50";
            //string publicIP = GetIPAddressString("vrtu-ms.eastus.cloudapp.azure.com");

            Random ran = new Random();
            byte[] buffer = new byte[100];
            ran.NextBytes(buffer);
            MbapHeader header = new MbapHeader()
            {
                UnitId = 1,
                ProtocolId = 1,
                TransactionId = 1,
                Length = 100
            };

            byte[] array = header.Encode();
            byte[] output = new byte[buffer.Length + array.Length];
            Buffer.BlockCopy(array, 0, output, 0, array.Length);
            Buffer.BlockCopy(buffer, 0, output, array.Length, buffer.Length);
            CancellationTokenSource cts = new CancellationTokenSource();
            IPEndPoint endpoint = new IPEndPoint(IPAddress.Parse(publicIP), 502);
            IChannel channel = ChannelFactory.Create(false, endpoint, 1024, 4048, cts.Token);
            channel.OnError += Channel_OnError;
            channel.OnClose += Channel_OnClose;
            channel.OnOpen += Channel_OnOpen;
            channel.OpenAsync().Wait();
            //channel.SendAsync(output).GetAwaiter();

            bool test = true;
            while(test)
            {
                Console.Write("Send a message [y/n] ? ");
                string decision = Console.ReadLine();
                if(decision.ToLowerInvariant() == "y")
                {
                    channel.SendAsync(output).GetAwaiter();
                }
                else
                {
                    test = false;
                }
            }

            Console.WriteLine("terminating...");
            Console.ReadKey();

        }

        private static string GetIPAddressString(string containerName)
        {
            IPHostEntry entry = Dns.GetHostEntry(containerName);


            string ipAddressString = null;

            foreach (var address in entry.AddressList)
            {
                if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    if (address.ToString().Contains("172"))
                    {
                        ipAddressString = address.ToString();
                        break;
                    }

                }
            }

            return ipAddressString;


        }


        private static void Channel_OnOpen(object sender, ChannelOpenEventArgs e)
        {
            Console.WriteLine("Channel is open");
        }

        private static void Channel_OnClose(object sender, ChannelCloseEventArgs e)
        {
            Console.WriteLine("Channel is closed");
        }

        private static void Channel_OnError(object sender, ChannelErrorEventArgs e)
        {
            Console.WriteLine($"Channel error - {e.Error.Message}");
        }
    }
}
