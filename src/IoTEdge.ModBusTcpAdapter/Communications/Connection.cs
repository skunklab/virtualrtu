using SkunkLab.Channels;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace IoTEdge.ModBusTcpAdapter.Communications
{
    public abstract class Connection : IDisposable
    {

        protected bool disposed;
        protected IChannel channel;
                     
        
        public virtual byte UnitId { get; set; }

        public abstract Task OpenAsync();

        public abstract Task SendAsync(byte[] message);

        public abstract Task CloseAsync();

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        
    }
}
