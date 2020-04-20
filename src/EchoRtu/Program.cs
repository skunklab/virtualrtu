using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using SkunkLab.Channels;

namespace EchoRtu
{
    internal class Program
    {
        private static TcpListener listener;

        private static IChannel channel;

        private static void Main(string[] args)
        {
            Console.WriteLine("8\"\"\"\"                    8\"\"\"8 \"\"8\"\" 8   8 ");
            Console.WriteLine("8     eeee e   e eeeee   8   8   8   8   8 ");
            Console.WriteLine("8eeee 8  8 8   8 8  88   8eee8e  8e  8e  8 ");
            Console.WriteLine("88    8e   8eee8 8   8   88   8  88  88  8 ");
            Console.WriteLine("88    88   88  8 8   8   88   8  88  88  8 ");
            Console.WriteLine("88eee 88e8 88  8 8eee8   88   8  88  88ee8");
            Console.WriteLine("");

            CancellationTokenSource cts = new CancellationTokenSource();
            IPAddress publicIP = GetIPAddress("localhost");

            listener = new TcpListener(publicIP, 503);
            listener.ExclusiveAddressUse = false;
            listener.Start();

            while (true)
            {
                TcpClient client = listener.AcceptTcpClientAsync().GetAwaiter().GetResult();
                client.LingerState = new LingerOption(false, 0);
                client.NoDelay = true;
                client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                client.Client.UseOnlyOverlappedIO = true;
                channel = ChannelFactory.Create(false, client, 1024, 4096, cts.Token);
                channel.OnClose += Channel_OnClose;
                channel.OnError += Channel_OnError;
                channel.OnOpen += Channel_OnOpen;
                channel.OnReceive += Channel_OnReceive;
                channel.OpenAsync().GetAwaiter();
            }


            //Console.WriteLine("Press any key to terminiate...");
            //Console.ReadKey();
        }

        private static async void Channel_OnReceive(object sender, ChannelReceivedEventArgs e)
        {
            Console.WriteLine("Message received");
            await channel.SendAsync(e.Message);
            Console.WriteLine("Message sent");
        }

        private static async void Channel_OnOpen(object sender, ChannelOpenEventArgs e)
        {
            Console.WriteLine("channel open");
            await channel.ReceiveAsync();
        }

        private static void Channel_OnError(object sender, ChannelErrorEventArgs e)
        {
            Console.WriteLine($"Error - {e.Error.Message}");
        }

        private static void Channel_OnClose(object sender, ChannelCloseEventArgs e)
        {
            Console.WriteLine("Closed connection");
        }

        private static IPAddress GetIPAddress(string hostname)
        {
            IPHostEntry hostInfo = Dns.GetHostEntry(hostname);
            for (int index = 0; index < hostInfo.AddressList.Length; index++)
            {
                if (hostInfo.AddressList[index].AddressFamily == AddressFamily.InterNetwork)
                {
                    return hostInfo.AddressList[index];
                }
            }

            return null;
        }
    }
}