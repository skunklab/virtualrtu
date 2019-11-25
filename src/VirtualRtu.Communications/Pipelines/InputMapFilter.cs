using VirtualRtu.Communications.Modbus;

namespace VirtualRtu.Communications.Pipelines
{
    public class InputMapFilter : IFilter
    {
        public InputMapFilter(MbapMapper mapper)
        {
            this.mapper = mapper;
        }

        private MbapMapper mapper;

        public byte[] Execute(byte[] message, byte? alias = null)
        {
            return mapper.MapIn(message, alias);
        }
    }
}
