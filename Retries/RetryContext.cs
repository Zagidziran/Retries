namespace Retries
{
    using System.Runtime.ExceptionServices;
    using Retries.Exceptions;

    public record RetryContext(uint RetryNumber, TimeSpan Elapsed, TimeSpan? TimeLeft, uint? TimesLeft, Exception? Error)
    {
        internal void ThrowIfInvalid()
        {
            if (this.TimeLeft < TimeSpan.Zero)
            {
                throw new TimeoutException();
            }

            if (this.TimesLeft == 0)
            {
                if (this.Error != null)
                {
                    ExceptionDispatchInfo.Capture(this.Error).Throw();
                }

                throw new NoMoreRetriesException();
            }
        }
    }
}
