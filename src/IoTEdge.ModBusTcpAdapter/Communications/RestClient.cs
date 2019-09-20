using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace IoTEdge.ModBusTcpAdapter.Communications
{
    public class RestClient
    {
        public RestClient(string containerName, int port, string path)
        {          
            this.path = path;
            CreateClient(containerName, port);
        }      

        private string path { get; set; }

        private string ipAddressString;
        private HttpClient client;

        public async Task SendAsync(byte[] message)
        {
            try
            {
                HttpResponseMessage response = await client.PostAsync(path, new ByteArrayContent(message));
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed http request - '{ex.Message}'");
            }
        }

        private void CreateClient(string containerName, int port)
        {
            client = new HttpClient();
            ipAddressString = GetIPAddressString(containerName);
            client.BaseAddress = new Uri(String.Format($"http://{ipAddressString}:{port}/"));
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/octet-stream"));
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



    }
}
