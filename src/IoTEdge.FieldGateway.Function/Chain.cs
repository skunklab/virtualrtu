using System.Threading.Tasks;

namespace IoTEdge.FieldGateway.Function
{
    public abstract class Chain
    {
        public virtual Chain UpdateObject { get; set; }
        public virtual async Task UpdateAsync()
        {
            await UpdateObject?.UpdateAsync();
        }
    }
}
