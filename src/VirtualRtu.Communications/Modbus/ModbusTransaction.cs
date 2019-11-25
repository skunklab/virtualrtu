namespace VirtualRtu.Communications.Modbus
{
    public class ModbusTransaction
    {
        private ushort id;
        private static ModbusTransaction instance;
        public static ModbusTransaction Create()
        {
            if (instance == null)
            {
                instance = new ModbusTransaction();
            }

            return instance;
        }

        public ushort Id
        {
            get
            {
                id++;
                if (id == 0)
                {
                    id++;
                }

                return id;
            }
        }
    }
}
