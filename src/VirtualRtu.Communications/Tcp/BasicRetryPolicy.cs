using System;
using System.Collections.Generic;
using System.Text;

namespace VirtualRtu.Communications.Tcp
{
    public class BasicRetryPolicy
    {
        public BasicRetryPolicy(ExponentialDelayPolicy delayPolicy, int maxRetries)
        {
            this.delayPolicy = delayPolicy;
            max = maxRetries;

        }

        private int max;
        private ExponentialDelayPolicy delayPolicy;

        public bool ShouldRetry(int retryCount)
        {
            return retryCount <= max;
        }

        public void Delay()
        {
            delayPolicy.Delay();
        }


    }
}
