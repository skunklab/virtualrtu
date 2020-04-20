using System.Threading.Tasks;

namespace VirtualRtu.WebMonitor.Hubs
{
    public interface ILogStream
    {
        Task SubscribeAsync(string resource, bool monitor);

        Task SubscribeAppInsightsAsync(string resource, bool monitor);
    }
}