using System;
using System.Collections.Generic;
using System.Net;

namespace IoTEdge.VRtu.FieldGateway.Communications
{
    public class IPHelper
    {
        public static string GetLocalAdapterRequestUrl(string containerName, int port, string path)
        {
            string ipAddressString = null;

            if(TryGetIPAddressString(containerName, out ipAddressString))
            {
                requestUrl = String.Format($"http://{ipAddressString}:{port}/{path}");
                Console.WriteLine("REQUEST URL = '{0}'", requestUrl);
                return requestUrl;
            }
            else
            {
                Console.WriteLine("NO IP ADDRESS FOUND USING LOCALHOST");
                return String.Format($"http://localhost:{port}/{path}");
            }
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


        private static bool TryGetIPAddressString(string containerName, out string ipAddressString)
        {
            try
            {
                ipAddressString = GetIPAddressString(containerName);
                return true;
            }
            catch
            {
                ipAddressString = null;
                return false;
            }
        }

        static IPHelper()
        {
            queue = new Queue<byte[]>();
            //IPHostEntry entry = Dns.GetHostEntry("mbpa");
            
            //string ipAddressString = null;

            //foreach (var address in entry.AddressList)
            //{
            //    if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            //    {
            //        if (address.ToString().Contains("172"))
            //        {
            //            ipAddressString = address.ToString();
            //            break;
            //        }

            //    }
            //}

            //if (ipAddressString != null)
            //{
            //    requestUrl = String.Format("http://{0}:8889/api/rtuinput", ipAddressString);
            //    Console.WriteLine("REQUEST URL = '{0}'", requestUrl);
            //}
            //else
            //{
            //    Console.WriteLine("NO IP ADDRESS FOUND");
            //}
        }

        private static Queue<byte[]> queue;
        private static string requestUrl;

        public static Queue<byte[]> Queue
        {
            get { return queue; }
        }
        public static string GetAddress()
        {
            return requestUrl;
        }



    }
}
