namespace Zagidziran.Retries
{
    using System.Runtime.CompilerServices;
    using Zagidziran.Retries.Exceptions;

    internal class RetryBuilder<T> : IRetryBuilder<T>
    {
        private readonly Func<CancellationToken, Task<T>> action;

        private readonly RetryPolicy<T> retryPolicy = new();

        public RetryBuilder(Func<CancellationToken, Task<T>> action)
        {
            this.action = action;
        }

        public IRetryBuilder<T> Until(Func<T, bool> check)
        {
            this.retryPolicy.FinishCheck = check;
            return this;
        }

        public IRetryBuilder<T> WithTimeout(TimeSpan timeout)
        {
            this.retryPolicy.Timeout = timeout;
            return this;
        }

        public IRetryBuilder<T> WithRetryInterval(TimeSpan interval)
        {
            this.retryPolicy.RetryInterval = interval;
            return this;
        }

        public IRetryBuilder<T> Times(uint times)
        {
            this.retryPolicy.Times = times;
            return this;
        }

        public IRetryBuilder<T> ShouldSatisfyTimes(uint times)
        {
            this.retryPolicy.ShouldSatisfyTimes = times;
            return this;
        }

        public IRetryBuilder<T> ShouldSatisfyDuring(TimeSpan interval)
        {
            this.retryPolicy.ShouldSatisfyInterval = interval;
            return this;
        }

        public IRetryBuilder<T> ReturnEvenFailed()
        {
            this.retryPolicy.ReturnEvenFailed = true;
            return this;
        }


        public IRetryBuilder<T> OnRetry(Func<RetryContext<T>, CancellationToken, Task> onRetry)
        {
            this.retryPolicy.OnRetry = onRetry;
            return this;
        }

        public IRetryBuilder<T> HandleException<TEx>(Func<TEx, bool>? filter = null)
            where TEx : Exception
        {
            this.retryPolicy.ExceptionsToHandle.Add(new RetryPolicy<T>.FilterRecord(typeof(TEx), AdaptFilter(filter)));
            return this;
        }

        public IRetryBuilder<T> Throw<TEx>(Func<TEx, bool>? filter)
            where TEx : Exception
        {
            this.retryPolicy.ExceptionsToThrow.Add(new RetryPolicy<T>.FilterRecord(typeof(TEx), AdaptFilter(filter)));
            return this;
        }

        public IRetryBuilder<T> WithCancellation(CancellationToken cancellationToken)
        {
            this.retryPolicy.CancellationToken = cancellationToken;
            return this;
        }

        internal TaskAwaiter<T> GetAwaiter()
        {
            return this.Run().GetAwaiter();
        }

        private async Task<T> Run()
        {
            var context = new RetryContext<T>(this.retryPolicy);
            while (true)
            {
                this.retryPolicy.CancellationToken.ThrowIfCancellationRequested();
                ContextValidationVerdict verdict;
                try
                {
                    var result = await this.action(this.retryPolicy.CancellationToken);

                    verdict = context.SetResult(result);
                }
                catch (Exception ex)
                {
                    verdict = context.SetError(ex);
                    if (verdict == ContextValidationVerdict.NeedThrow)
                    {
                        throw;
                    }
                }

                switch (verdict)
                {
                    case ContextValidationVerdict.NeedRetry:
                    case ContextValidationVerdict.NeedSatisfy:
                        break;

                    case ContextValidationVerdict.NoMoreRetries:
                        throw new NoMoreRetriesException();

                    case ContextValidationVerdict.Timeout:
                        throw new TimeoutException();

                    case ContextValidationVerdict.FailedButOkay:
                        return context.LastAvailableResult!;

                    case ContextValidationVerdict.Passed:
                        return context.LastAvailableResult!;
                }

                if (this.retryPolicy.OnRetry != null)
                {
                    await this.retryPolicy.OnRetry(context, this.retryPolicy.CancellationToken);
                }

                if (this.retryPolicy.RetryInterval.HasValue)
                {
                    await Task.Delay(this.retryPolicy.RetryInterval.Value, this.retryPolicy.CancellationToken);
                }

            }
        }

        private static Func<Exception, bool> AdaptFilter<TEx>(Func<TEx, bool>? filter)
            where TEx : Exception
        {
            if (filter == null)
            {
                return _ => true;
            }

            return ex => filter((TEx)ex);
        }
    }
}
