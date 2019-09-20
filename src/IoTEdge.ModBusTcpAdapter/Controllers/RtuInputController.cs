using IoTEdge.ModBusTcpAdapter.Communications;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace IoTEdge.ModBusTcpAdapter.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RtuInputController : ControllerBase
    {
        public RtuInputController(IConnection connection, ILogger<RtuInputController> logger)
        {
            this.connection = connection;
            this.logger = logger;
        }

        private IConnection connection;
        private ILogger<RtuInputController> logger;

        [HttpPost]
        public async Task<HttpResponseMessage> Post([FromBody] byte[] value)
        {
            try
            {
                await connection.SendAsync(value);
                return new HttpResponseMessage(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                logger?.LogError($"Fault publishing RTU message '{ex.Message}'");
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }
        }
    }
}
