using SkunkLab.Storage;
using System.Collections.Generic;
using VirtualRtu.Configuration.Tables;

namespace IoTEdge.VirtualRtu.WebMonitor.Configuration
{
    public class AssetConfiguration
    {
        public static GraphAssets Load(string tableName, string connectionString)
        {
            var assets = new GraphAssets();
            TableStorage tableStorage = TableStorage.New(connectionString);
            List<ContainerEntity> gatewayList = tableStorage.ReadAsync<ContainerEntity>(tableName).GetAwaiter().GetResult();
            foreach (var item in gatewayList)
            {
                assets.Add(item.VirtualRtuId, item.DeviceId);
            }


            return assets;

        }

    }
}
