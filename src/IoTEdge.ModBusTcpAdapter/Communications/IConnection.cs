using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace IoTEdge.ModBusTcpAdapter.Communications
{
    public interface IConnection
    {
        Task SendAsync(byte[] message);
    }
}
