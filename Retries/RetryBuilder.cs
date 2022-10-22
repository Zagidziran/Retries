using System.Runtime.CompilerServices;

namespace Retries
{
    using System.Diagnostics;

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

        public IRetryBuilder<T> OnRetry(Func<RetryContext, CancellationToken, Task> onRetry)
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
            var timer = Stopwatch.StartNew();
            uint retryNumber = 0;
            while (true)
            {
                this.retryPolicy.CancellationToken.ThrowIfCancellationRequested();
                RetryContext context = default!;
                try
                {
                    var result = await this.action(this.retryPolicy.CancellationToken);
                    if (this.retryPolicy.FinishCheck?.Invoke(result) ?? true)
                    {
                        return result;
                    }

                    context = this.CreateRetryContext(timer, retryNumber, null);
                }
                catch (Exception ex)
                {
                    if (!this.retryPolicy.IsExceptionShouldBeHandled(ex))
                    {
                        throw;
                    }

                    context = this.CreateRetryContext(timer, retryNumber, ex);

                }

                if (this.retryPolicy.OnRetry != null)
                {
                    await this.retryPolicy.OnRetry(context, this.retryPolicy.CancellationToken);
                }

                if (this.retryPolicy.RetryInterval.HasValue)
                {
                    await Task.Delay(this.retryPolicy.RetryInterval.Value, this.retryPolicy.CancellationToken);
                }

                retryNumber++;
            }
        }

        private RetryContext CreateRetryContext(Stopwatch timer, uint retryNumber, Exception? ex)
        {
            var elapsed = timer.Elapsed;
            var timeLeft = this.retryPolicy.Timeout - elapsed;
            var timesLeft = this.retryPolicy.Times - retryNumber;
            var retryContext = new RetryContext(retryNumber, timer.Elapsed, timeLeft, timesLeft, ex);
            retryContext.ThrowIfInvalid();
            return retryContext;
        }

        private static Func<Exception, bool> AdaptFilter<TEx>(Func<TEx, bool>? filter)
            where TEx : Exception
        {
            if (filter == null)
            {
                return _ => true;
            }

            return ex => filter((TEx) ex);
        }
    }
}
