using IoTEdge.VirtualRtu.WebMonitor.Configuration;

namespace IoTEdge.VirtualRtu.WebMonitor.Models
{
    public class AssetTree
    {
        public AssetTree(GraphAssets assets)
        {
            this.assets = assets;
        }

        private GraphAssets assets;


    }
}
