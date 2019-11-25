using VirtualRtu.Communications.Modbus;

namespace VirtualRtu.Communications.Pipelines
{
    public class OutputMapFilter : IFilter
    {
        public OutputMapFilter(MbapMapper mapper)
        {
            this.mapper = mapper;
        }

        private MbapMapper mapper;

        public byte[] Execute(byte[] message, byte? alias = null)
        {
            return mapper.MapOut(message);
        }
    }
}
