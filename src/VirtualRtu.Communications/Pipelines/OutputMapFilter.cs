using VirtualRtu.Communications.Modbus;

namespace VirtualRtu.Communications.Pipelines
{
    public class OutputMapFilter : IFilter
    {
        private readonly MbapMapper mapper;

        public OutputMapFilter(MbapMapper mapper)
        {
            this.mapper = mapper;
        }

        public byte[] Execute(byte[] message, byte? alias = null)
        {
            return mapper.MapOut(message);
        }
    }
}