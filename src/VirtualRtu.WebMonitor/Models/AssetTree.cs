using VirtualRtu.WebMonitor.Configuration;

namespace VirtualRtu.WebMonitor.Models
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
