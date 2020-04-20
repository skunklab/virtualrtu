using VirtualRtu.Communications.Modbus;

namespace VirtualRtu.Communications.Pipelines
{
    public class InputMapFilter : IFilter
    {
        private readonly MbapMapper mapper;

        public InputMapFilter(MbapMapper mapper)
        {
            this.mapper = mapper;
        }

        public byte[] Execute(byte[] message, byte? alias = null)
        {
            return mapper.MapIn(message, alias);
        }
    }
}