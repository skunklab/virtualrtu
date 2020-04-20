using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SkunkLab.Channels;
using VirtualRtu.Communications.Channels;
using VirtualRtu.Communications.Modbus;
using VirtualRtu.Communications.Tcp;
using VirtualRtu.Configuration;

namespace VirtualRtu.Gateway.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RtuController : ControllerBase
    {
        private readonly WaitHandle[] waitHandles =
        {
            new AutoResetEvent(false)
        };

        private readonly IChannel channel;
        private readonly ILogger logger;


        private readonly MbapMapper mapper;

        private byte[] result;

        public RtuController(ModuleConfig config, ILogger logger = null)
        {
            channel = ModuleTcpChannel.CreateSingleton(config, logger);
            if (!channel.IsConnected)
            {
                channel.OpenAsync().GetAwaiter();
            }

            this.logger = logger;
        }

        public RtuController(ModuleConfig config, ModuleTcpChannel channel, ILogger logger = null)
        {
            if (channel == null)
            {
                this.channel = ModuleTcpChannel.CreateSingleton(config, logger);
                if (!this.channel.IsConnected)
                {
                    this.channel.OpenAsync().GetAwaiter();
                }
            }
            else
            {
                this.channel = channel;
            }

            this.logger = logger;
            mapper = new MbapMapper(Guid.NewGuid().ToString());
        }

        private event HttpResponseObserverHandler OnMessage;

        [HttpPost]
        [Produces("application/octet-stream")]
        public async Task<HttpResponseMessage> Post([FromBody] byte[] message)
        {
            channel.OnReceive += Channel_OnReceive;
            mapper.MapIn(message);
            await channel.AddMessageAsync(message);
            ThreadPool.QueueUserWorkItem(Listen, waitHandles[0]);
            WaitHandle.WaitAll(waitHandles);
            if (result != null)
            {
                logger?.LogDebug("API returned response.");
                return new HttpResponseMessage(HttpStatusCode.OK) {Content = new ByteArrayContent(result)};
            }

            logger?.LogWarning("API returned no response from RTU.");
            return new HttpResponseMessage(HttpStatusCode.InternalServerError);
        }

        private void Channel_OnReceive(object sender, ChannelReceivedEventArgs e)
        {
            OnMessage?.Invoke(this, new TcpReceivedEventArgs(e.ChannelId, e.Message));
        }

        private void Listen(object state)
        {
            AutoResetEvent are = (AutoResetEvent) state;
            OnMessage += (o, a) =>
            {
                byte[] msg = mapper.MapOut(a.Message);
                if (msg != null)
                {
                    result = a.Message;
                    are.Set();
                }
            };
        }

        private delegate void HttpResponseObserverHandler(object sender, TcpReceivedEventArgs args);
    }
}