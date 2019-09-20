using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using IoTEdge.VirtualRtu.Configuration;
using IoTEdge.VirtualRtu.FieldGateway.Communications;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Piraeus.Clients.Mqtt;
using SkunkLab.Protocols.Mqtt;

namespace IoTEdge.VirtualRtu.FieldGateway.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RtuOutputController : ControllerBase
    {
        //private string requestUrl; // = "http://echomodule:8889/api/rtuinput";
      

        public RtuOutputController(EdgeGatewayConfiguration config, ILogger<RtuOutputController> logger)
        {
            this.logger = logger;
            try
            {
                director = CommunicationDirector.Create(config);
            }
            catch(Exception ex)
            {
                logger.LogError(ex.Message);
            }

        }

        private CommunicationDirector director;
        private ILogger<RtuOutputController> logger;

        [HttpPost]
        public async Task<HttpResponseMessage> Post([FromBody] byte[] value)
        {
            try
            {
                Console.WriteLine("Received message MBPA");
                await director.SendRtuOutputAsync(value);
                return new HttpResponseMessage(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                logger.LogError($"Fault publishing MQTT message '{ex.Message}'");
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }
        }
    }
}
