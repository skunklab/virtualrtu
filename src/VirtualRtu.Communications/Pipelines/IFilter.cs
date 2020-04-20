namespace VirtualRtu.Communications.Pipelines
{
    public interface IFilter
    {
        byte[] Execute(byte[] message, byte? alias = null);
    }
}