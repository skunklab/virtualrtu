namespace VirtualRtu.Communications.Tcp
{
    public class BasicRetryPolicy
    {
        private readonly ExponentialDelayPolicy delayPolicy;

        private readonly int max;

        public BasicRetryPolicy(ExponentialDelayPolicy delayPolicy, int maxRetries)
        {
            this.delayPolicy = delayPolicy;
            max = maxRetries;
        }

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