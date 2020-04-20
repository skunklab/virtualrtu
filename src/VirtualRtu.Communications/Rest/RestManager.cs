namespace VirtualRtu.Communications.Rest
{
    public class RestManager
    {
        //public RestManager(TcpManager tcpManager, ILogger logger = null)
        //{
        //    this.tcpManager = tcpManager;
        //    this.logger = logger;
        //    this.mapper = new MbapMapper("restCache");
        //    this.tcpManager.OnReceived += TcpManager_OnReceived;
        //}

        //private TcpManager tcpManager;
        //public event EventHandler<TcpReceivedEventArgs> OnMessage;
        //private MbapMapper mapper;
        //private ILogger logger;

        //public async Task SendAsync(string id, byte[] message)
        //{
        //    mapper.MapIn(message);
        //    await tcpManager.SendAsync(message);
        //    logger?.LogDebug("Message sent to tcp manager from rest manager.");
        //}

        //private void TcpManager_OnReceived(object sender, TcpReceivedEventArgs e)
        //{
        //    if(e.Message != null)
        //    {
        //        byte[] msg = mapper.MapOut(e.Message);
        //        if(msg != null)
        //        {
        //            OnMessage?.Invoke(this, new TcpReceivedEventArgs(e.Id, msg));
        //            logger?.LogDebug("Rest manager returning message.");
        //        }
        //        else
        //        {
        //            logger?.LogDebug("Rest manager received null message.");
        //        }
        //    }
        //    else
        //    {
        //        logger?.LogDebug("Message received by rest manager and mismatched.");
        //    }
        //}
    }
}