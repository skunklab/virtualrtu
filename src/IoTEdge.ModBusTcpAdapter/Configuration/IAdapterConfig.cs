using System;
using System.Collections.Generic;
using System.Text;

namespace IoTEdge.ModBusTcpAdapter.Configuration
{
    public interface IAdapterConfig
    {
        SlaveConfig[] Slaves { get; set; }
        string FieldGatewayContainerName { get; set; }
        int FieldGatewayPort { get; set; }
        string FieldgatewayPath { get; set; }
    }
}
