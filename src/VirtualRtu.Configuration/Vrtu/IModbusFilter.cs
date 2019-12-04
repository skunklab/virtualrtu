namespace VirtualRtu.Configuration.Vrtu
{
    public interface IModbusFilter
    {
        bool Apply(ushort address, ushort qty, byte scope);
    }
}
