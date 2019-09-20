using System;
using System.Collections.Generic;
using System.Text;
using System.Transactions;

namespace IoTEdge.ModBusTcpAdapter.Communications
{
    internal class Transaction
    {
        private ushort id;
        private static Transaction instance;
        public static Transaction Create()
        {
            if(instance == null)
            {
                instance = new Transaction();
            }

            return instance;
        }

        public ushort Id
        {
            get
            {
                id++;
                if(id == 0)
                {
                    id++;
                }

                return id;
            }
        }
    }
}
