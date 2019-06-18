using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using IoTEdge.VirtualRtu.Web.Models;
using System;

namespace IoTEdge.VirtualRtu.Web.Controllers
{
    public class HomeController : Controller
    {
        private IConfiguration configuration;
        private string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcefghijklmnopqrtstuvwxyz0123456789";

        public HomeController(IConfiguration configuration)
        {
            this.configuration = configuration;
        }
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Index(int unitId, string virtualRtuId, string deviceId, string modbusContainerName, int modbusPort, string modbusPath, int expirationMinutes)
        {
            try
            {
                DateTime created = DateTime.UtcNow;
                DateTime expiration = created.AddMinutes(expirationMinutes);

                string luss = GetLuss();

                //update the table entity and return the luss

                //LussEntity entity = new LussEntity()
                //{
                //    Luss = luss,
                //    DeviceId = deviceId,
                //    ModuleId = moduleId,
                //    UnitId = unitId,
                //    VirtualRtuId = virtualRtuId,
                //    Created = created,
                //    Expires = expiration
                //};

                //string tableName = configuration.GetValue<string>("TableName");
                //string connectionString = configuration.GetConnectionString("AzureStorageConnectionString");
                //Task task = entity.UpdateAsync(tableName, connectionString);
                //Task.WhenAll(task);

                //ViewBag.LUSS = luss;
            }
            catch (Exception ex)
            {
                //Trace.TraceError(ex.InnerException.Message);
            }

            return View();
        }

        private string GetLuss()
        {
            int len = alphabet.Length;
            
        }
    }
}
