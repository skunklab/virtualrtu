namespace VirtualRtu.Communications.Modbus
{
    public class ModbusTransaction
    {
        private static ModbusTransaction instance;
        private ushort id;

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

        public static ModbusTransaction Create()
        {
            if (instance == null)
            {
                instance = new ModbusTransaction();
            }

            return instance;
        }
    }
}