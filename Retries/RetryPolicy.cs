namespace Retries
{
    internal class RetryPolicy<T>
    {
        public Func<T, bool>? FinishCheck { get; set; }

        public TimeSpan? Timeout { get; set; }

        public TimeSpan? RetryInterval { get; set; }

        public uint? Times { get; set; }

        public Func<RetryContext, CancellationToken, Task>? OnRetry { get; set; }

        public List<FilterRecord> ExceptionsToHandle { get; } = new ();

        public List<FilterRecord> ExceptionsToThrow { get; } = new();

        public CancellationToken CancellationToken { get; set; } = CancellationToken.None;

        public bool IsExceptionShouldBeHandled(Exception ex)
        {
            var shouldBeThrown = this.ExceptionsToThrow.Any(filter => filter.IsMatched(ex));
            if (shouldBeThrown)
            {
                return false;
            }

            return this.ExceptionsToHandle.Any(filter => filter.IsMatched(ex));
        }

        internal record FilterRecord(Type ExceptionType, Func<Exception, bool> Filter)
        {
            public bool IsMatched(Exception ex)
            {
                return ex.GetType().IsAssignableTo(this.ExceptionType) && Filter(ex);
            }
        }
    }
}
