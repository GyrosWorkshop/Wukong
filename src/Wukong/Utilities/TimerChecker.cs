using Wukong.Services;
using Nito.AsyncEx;

namespace Wukong.Utilities
{
    class TimerChecker
    {
        private int invokeCount = 0;
        private int max;
        private AsyncAutoResetEvent resetEvent;

        public TimerChecker(int max, AsyncAutoResetEvent resetEvent)
        {
            this.max = max;
            this.resetEvent = resetEvent;
        }

        public void Check(object state)
        {
            var channel = (Channel)state;
            if (channel.IsIdle || invokeCount == max)
            {
                resetEvent.Set();
            }
            invokeCount += 1;
        }
    }
}