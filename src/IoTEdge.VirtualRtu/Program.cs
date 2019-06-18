using IoTEdge.VirtualRtu.Configuration;
using IoTEdge.VirtualRtu.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace IoTEdge.VirtualRtu
{
    class Program
    {
        private static ManualResetEventSlim done;
        private static VirtualRtuConfiguration config;
        private static ListenerService listenerService;
        private static Startup su;

        static void Main(string[] args)
        {
            //virtual rtu configures itself, then starts the tcp server on port 502 (modbus-tcp)
            //a scada client will connect over 502 uses the IP address or dns name of this virtual rtu

            //once a connection is made the v-rtu will read the RtuMap and determine if a matching UnitID
            //exists in the ModBusTcp header. 

            Console.WriteLine("---------------------------------------------");
            Console.WriteLine("------------Starting Virtual RTU ----------");
            Console.WriteLine("---------------------------------------------");

            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

            su = new Startup();
            su.Configure();


            //IConfigurationBuilder builder = new ConfigurationBuilder();            
            //Configure(builder);


            // create service collection
            var serviceCollection = new ServiceCollection();
            //ConfigureServices(serviceCollection);
            su.ConfigureServices(serviceCollection);

            // create service provider
            var serviceProvider = serviceCollection.BuildServiceProvider();

            // entry to run app
            listenerService = serviceProvider.GetService<ListenerService>();
            listenerService.OnError += ListenerService_OnError;
            Task task = listenerService.RunAsync();
            Task.WhenAll(task);


            done = new ManualResetEventSlim(false);

            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                done.Set();
                eventArgs.Cancel = true;
            };

            Console.WriteLine("Virtual RTU is listening...");
            done.Wait();
        }

        

        private static void ListenerService_OnError(object sender, ListenerErrorEventArgs e)
        {
            Console.WriteLine($"Listener error - {e.Error.Message}");
            done.Set();
        }


        public static void Configure(IConfigurationBuilder builder)
        {
            try
            {
                builder.AddJsonFile("./secrets.json");
                builder.AddEnvironmentVariables("VRTU_");

                IConfigurationRoot root = builder.Build();

                config = new VirtualRtuConfiguration();
                ConfigurationBinder.Bind(root, config);
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Configure error - {ex.Message}");
                throw ex;
            }
        }

        private static void ConfigureServices(IServiceCollection serviceCollection)
        {
            try
            {
                serviceCollection.AddSingleton<VirtualRtuConfiguration>(config);
                serviceCollection.AddTransient<ListenerService>();
            }
            catch(Exception ex)
            {
                Console.WriteLine($"ConfigureServices error - {ex.Message}");
                throw ex;
            }
           
        }


        private static void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            Console.WriteLine("---- Unobserved exception ----");
            Console.WriteLine($"Error - {e.Exception.Message}");
            Console.WriteLine($"Stack Trace - {e.Exception.StackTrace}");
        }
    }
}
