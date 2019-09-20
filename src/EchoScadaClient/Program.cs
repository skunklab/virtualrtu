
using IoTEdge.VirtualRtu.Configuration;
using Microsoft.IdentityModel.JsonWebTokens;
using Newtonsoft.Json;
using SkunkLab.Channels;

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EchoScadaClient
{
    class Program
    {
        public static IChannel channel;
        public static bool ican;
        static void Main(string[] args)
        {
            //string jwtString = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2t1bmtsYWIuaW8vbmFtZSI6ImRldmljZTEiLCJodHRwOi8vc2t1bmtsYWIuaW8vcm9sZSI6ImRlbW92cnR1IiwibmJmIjoxNTYzMzE4NDIwLCJleHAiOjE1OTQ4NTQ0MjAsImlhdCI6MTU2MzMxODQyMCwiaXNzIjoiaHR0cDovL3NrdW5rbGFiLmlvLyIsImF1ZCI6Imh0dHA6Ly9za3Vua2xhYi5pby8ifQ.KgVm8g2dWp_IlBbMlFNvojPQo594BmE-3VCAh9Nwnso";

            //string jwtString = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2t1bmtsYWIuaW8vbmFtZSI6ImRldmljZTciLCJodHRwOi8vc2t1bmtsYWIuaW8vcm9sZSI6ImRlbW92cnR1IiwibmJmIjoxNTYwMTg2OTQ3LCJleHAiOjE1OTE3MjI5NDcsImlhdCI6MTU2MDE4Njk0NywiaXNzIjoiaHR0cDovL3NrdW5rbGFiLmlvLyIsImF1ZCI6Imh0dHA6Ly9za3Vua2xhYi5pby8ifQ.JgvXZfn36XQ9CQPzt_7DNLVlJCX-_g6zmG3wc6TFHWk";
            //string jwtString = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2t1bmtsYWIuaW8vbmFtZSI6InZydHUiLCJodHRwOi8vc2t1bmtsYWIuaW8vcm9sZSI6Im1hbmFnZSIsIm5iZiI6MTU2MDE4ODI1NywiZXhwIjoxNTkxNzI0MjU3LCJpYXQiOjE1NjAxODgyNTcsImlzcyI6Imh0dHA6Ly9za3Vua2xhYi5pby8iLCJhdWQiOiJodHRwOi8vc2t1bmtsYWIuaW8vIn0.Vkx3MLxymnlf_xlFN0l8Yt_PoqwtsugnEtRmmqOApDE";
            //string jwtString = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2t1bmtsYWIuaW8vbmFtZSI6ImRlbW92cnR1LWRldmljZTEiLCJuYmYiOjE1NjAyNTQ1OTcsImV4cCI6MTU5MTc5MDU5NywiaWF0IjoxNTYwMjU0NTk3LCJpc3MiOiJodHRwOi8vc2t1bmtsYWIuaW8vIiwiYXVkIjoiaHR0cDovL3NrdW5rbGFiLmlvLyJ9.mlYROEu9jlq_bAs7M894al8hcM_9ebGd4Cmy9Tfi1ws";
            //string jwtString = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2t1bmtsYWIuaW8vbmFtZSI6ImRlbW92cnR1LWRldmljZTEiLCJuYmYiOjE1NjAyNjM0MDcsImV4cCI6MTU5MTc5OTQwNywiaWF0IjoxNTYwMjYzNDA3LCJpc3MiOiJodHRwOi8vc2t1bmtsYWIuaW8vIiwiYXVkIjoiaHR0cDovL3NrdW5rbGFiLmlvLyJ9.K_RtV9d7tZ1qSbyy4PHZVmGcC4Uq3D4EjrL7BWY3Sg0";
            //JsonWebToken jwt = new JsonWebToken(jwtString);
            //string key = "//////////////////////////////////////////8=";
            //List<Claim> list = new List<Claim>();
            //list.Add(new Claim("http://skunklab.io/name", "device1"));
            //list.Add(new Claim("http://skunklab.io/role", "demovrtu"));
            //SkunkLab.Security.Tokens.JsonWebToken j = new SkunkLab.Security.Tokens.JsonWebToken(key, list, 525600.0, "http://skunklab.io/", "http://skunklab.io/");
            //string jtoken = j.ToString();

            



            Console.WriteLine("----Test SCADA Echo Client-----");
            Console.WriteLine("press any key to continue");
            Console.ReadKey();

            //Console.WriteLine("Enter VRTU IP address or hostname ? ");

            //string publicIP = GetIPAddressString(System.Net.Dns.GetHostName());
            //string publicIP = "192.168.168.54";
            //13.82.175.48

            string publicIP = "168.62.59.20"; //Schneider (latest)
            //string publicIP = "40.85.191.244";  //Schneider

            Random ran = new Random();
            byte[] buffer = new byte[100];
            ran.NextBytes(buffer);
            MbapHeader header = new MbapHeader()
            {
                UnitId = 2,
                ProtocolId = 1,
                TransactionId = 1,
                Length = 6
            };

            byte[] body = new byte[] { 3, 79, 27, 0, 10 };



            byte[] array = header.Encode();
            byte[] output = new byte[buffer.Length + array.Length];
            Buffer.BlockCopy(array, 0, output, 0, array.Length);
            Buffer.BlockCopy(buffer, 0, output, array.Length, buffer.Length);

            //byte[] o2  = Convert.FromBase64String("AAMAAAAGAQNPGwAK");
            //byte[] o2 = Convert.FromBase64String("AAUAAAAGAQNPGwAK");
            //byte[] o2 = Convert.FromBase64String("AAUAAAAGAgNPGwAK");
            byte[] o2 = Convert.FromBase64String("AAEAAAAGAQNPGwAK");
            MbapHeader mh = MbapHeader.Decode(o2);

            output = o2;
            string x = BitConverter.ToString(o2);

            CancellationTokenSource cts = new CancellationTokenSource();
            IPEndPoint endpoint = new IPEndPoint(IPAddress.Parse(publicIP), 502);
            channel = ChannelFactory.Create(false, endpoint, 1024, 4048, cts.Token);
            channel.OnError += Channel_OnError;
            channel.OnClose += Channel_OnClose;
            channel.OnOpen += Channel_OnOpen;
            channel.OnReceive += Channel_OnReceive;

            channel.OpenAsync().GetAwaiter();
            while(!ican)
            {
                Task t = Task.Delay(5000);
                Task.WaitAll(t);
            }
            
            //channel.SendAsync(output).GetAwaiter();

            bool test = true;
            byte dummy = 99;
            while(test)
            {
                Console.Write("Send a message [y/n] ? ");
                
                string decision = Console.ReadLine();
                if(decision.ToLowerInvariant() == "y")
                {
                    byte[] payload = Convert.FromBase64String("AAEAAAAGAQNPGwAK");                   
                    //dummy++;
                    //payload[1] = dummy;

                    string bc = BitConverter.ToString(payload);
                    channel.SendAsync(payload).GetAwaiter();
                    //channel.SendAsync(output).GetAwaiter();
                    Console.WriteLine($"Sent message length {output.Length}");
                }
                else
                {
                    test = false;
                }
            }

            Console.WriteLine("terminating...");
            Console.ReadKey();

        }

        private static void Channel_OnReceive(object sender, ChannelReceivedEventArgs e)
        {
            Console.WriteLine($"Received message length {e.Message.Length}");
        }

        private static string GetIPAddressString(string containerName)
        {
            IPHostEntry entry = Dns.GetHostEntry(containerName);


            string ipAddressString = null;

            foreach (IPAddress address in entry.AddressList)
            {                
                if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    Console.WriteLine(address.ToString());
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
            channel.ReceiveAsync();
            ican = true;
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
