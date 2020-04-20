namespace VirtualRtu.Configuration.Vrtu
{
    public enum ModbusMessageType
    {
        Unknown = 0,
        ReadCoilStatus = 1,
        ReadInputStatus = 2,
        ReadHoldingRegisters = 3,
        ReadInputRegisters = 4,
        WriteSingleCoil = 5,
        WriteSingleRegister = 6,
        Diagnostics = 8,
        FetchEventCounter = 11,
        FetchEventLog = 12,
        WriteMultipleCoils = 15,
        WriteMultipleRegisters = 16
    }
}