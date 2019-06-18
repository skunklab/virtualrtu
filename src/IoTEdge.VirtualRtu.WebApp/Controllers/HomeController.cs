using IoTEdge.VirtualRtu.Configuration;
using IoTEdge.VirtualRtu.WebApp.Configuration;
using IoTEdge.VirtualRtu.WebApp.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Diagnostics;
using System.Text;

namespace IoTEdge.VirtualRtu.WebApp.Controllers
{
    public class HomeController : Controller
    {
        private WebAppConfig config;
        private string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcefghijklmnopqrtstuvwxyz0123456789";

        public HomeController(WebAppConfig config)
        {
            this.config = config;
        }

        public IActionResult Index()
        {
            return View(new RtuModel());
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpPost]
        public ActionResult Index(int unitId, string virtualRtuId, string deviceId, string hostname, string modbusContainerName, int modbusPort, string modbusPath, int expirationMinutes)
        {
            try
            {
                DateTime created = DateTime.UtcNow;
                DateTime expiration = created.AddMinutes(expirationMinutes);

                string luss = GetLuss();

                //update the table entity and return the luss

                LussEntity entity = new LussEntity()
                {
                    Created = created,
                    DeviceId = deviceId,
                    Luss = luss,
                    Expires = expiration,
                    UnitId = unitId,
                    VirtualRtuId = virtualRtuId,
                    ModbusContainer = modbusContainerName,
                    ModbusPort = modbusPort,
                    ModbusPath = modbusPath,
                    Hostname = hostname                  
                };

                entity.UpdateAsync(config.TableName, config.StorageConnectionString).GetAwaiter();

                ViewBag.LUSS = luss;
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.InnerException.Message);
            }

            return View();
        }


        private string GetLuss()
        {
            int len = alphabet.Length - 1;
            Random ran = new Random();
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < 32; i++)
            {
                int id = ran.Next(0, len);
                builder.Append(alphabet[id]);
            }

            return builder.ToString();
        }
    }
}
