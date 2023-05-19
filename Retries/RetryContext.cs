namespace Zagidziran.Retries
{
    public class RetryContext<T>
    {
        private readonly RetryPolicy<T> policy;

        private uint satisfidTimesInRow;

        private DateTimeOffset? satisfiedFrom;

        private T? lastAvailableResult;

        internal RetryContext(RetryPolicy<T> policy)
        {
            this.policy = policy;
        }

        public DateTimeOffset StartedAt { get; } = DateTimeOffset.Now;

        public TimeSpan Elapsed => DateTimeOffset.Now - this.StartedAt;

        public Exception? Error { get; private set; }

        public T? LastAvailableResult
        {
            get => this.lastAvailableResult;
            private set
            {
                this.lastAvailableResult = value;
                this.HasResult = true;
            }
        }

        // We need handle missing result as far as generic nullable value types actually not nullable when no generic constraint.
        // See https://learn.microsoft.com/en-us/dotnet/csharp/nullable-references
        public bool HasResult { get; private set; }

        public uint RetryNumber { get; private set; }

        internal ContextValidationVerdict SetResult(T result)
        {
            this.LastAvailableResult = result;
            this.Error = null;
            this.RetryNumber++;

            var checkResult = policy.FinishCheck?.Invoke(result) ?? true;

            if (checkResult)
            {
                this.satisfiedFrom ??= DateTimeOffset.Now;
                this.satisfidTimesInRow++;
            }
            else
            {
                this.satisfiedFrom = null;
                this.satisfidTimesInRow = 0;

                if (policy.Times != null && this.RetryNumber > policy.Times)
                {
                    return policy.ReturnEvenFailed && this.HasResult 
                        ? ContextValidationVerdict.FailedButOkay
                        : ContextValidationVerdict.NoMoreRetries;
                }

                return this.CheckForTimeout(ContextValidationVerdict.NeedRetry);
            }

            if (policy.ShouldSatisfyInterval != null && DateTimeOffset.Now - this.satisfiedFrom < policy.ShouldSatisfyInterval)
            {
                return this.CheckForTimeout(ContextValidationVerdict.NeedSatisfy);
            }

            if (policy.ShouldSatisfyTimes != null && this.satisfidTimesInRow < policy.ShouldSatisfyTimes)
            {
                return this.CheckForTimeout(ContextValidationVerdict.NeedSatisfy);
            }

            return ContextValidationVerdict.Passed;
        }

        internal ContextValidationVerdict SetError(Exception ex)
        {
            this.Error = ex;
            this.RetryNumber++;
            this.satisfidTimesInRow = 0;
            this.satisfiedFrom = null;

            var throwVerdict = CheckForReturnIfFailed(ContextValidationVerdict.NeedThrow);
            var retryVerdict = ContextValidationVerdict.NeedRetry;

            if (policy.ExceptionsToThrow.Any(filter => filter.IsMatched(this.Error)))
            {
                return throwVerdict;
            }

            // And need to trow if exception is not handleable.
            if (!policy.ExceptionsToHandle.Any(filter => filter.IsMatched(this.Error)))
            {
                return throwVerdict;
            }

            if (policy.Timeout != null && this.Elapsed >= policy.Timeout)
            {
                return throwVerdict;
            }

            if (policy.Times != null && this.RetryNumber > policy.Times)
            {
                return throwVerdict;
            }

            return retryVerdict;
        }

        private ContextValidationVerdict CheckForTimeout(ContextValidationVerdict verdictCandidate)
        {
            if (policy.Timeout != null && this.Elapsed >= policy.Timeout)
            {
                return policy.ReturnEvenFailed && this.HasResult
                    ? ContextValidationVerdict.FailedButOkay
                    : ContextValidationVerdict.Timeout;
            }

            return verdictCandidate;
        }

        private ContextValidationVerdict CheckForReturnIfFailed(ContextValidationVerdict verdictCandidate)
        {
            return policy.ReturnEvenFailed && this.HasResult
                ? ContextValidationVerdict.FailedButOkay
                : verdictCandidate;
        }
    }
}
