﻿using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SkunkLab.Channels;

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using VirtualRtu.Communications.Modbus;

namespace EchoScadaClient
{
    class Program
    {
        public static IChannel channel;
        public static bool ican;
        static void Main(string[] args)
        {
            Console.WriteLine("8\"\"\"\"8 8\"\"\"\"8 8\"\"\"\"8 8\"\"\"\"8 8\"\"\"\"8");
            Console.WriteLine("8      8    \" 8    8 8    8 8    8");
            Console.WriteLine("8eeeee 8e     8eeee8 8e   8 8eeee8 ");
            Console.WriteLine("    88 88     88   8 88   8 88   8");
            Console.WriteLine("e   88 88   e 88   8 88   8 88   8 ");
            Console.WriteLine("8eee88 88eee8 88   8 88eee8 88   8");
            Console.WriteLine("");
            Console.WriteLine("eeee e     eeee e  eeeee eeeee ");
            Console.WriteLine("8  8 8     8    8  8   8   8");
            Console.WriteLine("8e   8e    8eee 8e 8e  8   8e");
            Console.WriteLine("88   88    88   88 88  8   88");
            Console.WriteLine("88e8 88eee 88ee 88 88  8   88 ");
            Console.WriteLine("");
           
            Console.WriteLine("press any key to continue");
            Console.ReadKey();

            string publicIP = "168.62.59.20";
            Console.Write("Enter for default IP (127.0.0.1)? ");
            string inputIpAddress = Console.ReadLine();
            if(!string.IsNullOrEmpty(inputIpAddress))
            {
                publicIP = inputIpAddress;
            }
            else
            {
                publicIP = "127.0.0.1";
            }


            Random ran = new Random();
            byte[] buffer = new byte[100];
            ran.NextBytes(buffer);
            MbapHeader header = new MbapHeader()
            {
                UnitId = 1,
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
