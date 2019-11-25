using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace VirtualRtu.Communications.Tcp
{
    public class ExponentialDelayPolicy
    {
        public ExponentialDelayPolicy(int maxSeconds, int entropy = 5, bool randomness = true)
        {
            max = maxSeconds;
            randomize = randomness;
            this.entropy = entropy;
            if (randomize)
            {
                ran = new Random();
            }
        }

        private int max;
        private bool randomize;
        private int entropy;
        private int index;
        private Random ran;

        public void Delay()
        {
            if (Math.Pow(Math.E, index + 1) > Convert.ToDouble(max))
            {
                index = 0;
            }

            double delay = Math.Pow(Math.E, index + 1);

            if (randomize)
            {
                int offset = ran.Next(0, entropy);
                delay += Convert.ToDouble(offset);
            }

            index++;
            Task.Delay(TimeSpan.FromSeconds(delay)).Wait();
        }

    }
}
