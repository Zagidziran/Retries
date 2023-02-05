namespace Zagidziran.Retries
{
    public interface IRetryBuilder<T>
    {
        IRetryBuilder<T> Until(Func<T, bool> check);

        IRetryBuilder<T> WithTimeout(TimeSpan timeout);

        IRetryBuilder<T> WithRetryInterval(TimeSpan interval);

        IRetryBuilder<T> Times(uint times);

        IRetryBuilder<T> OnRetry(Func<RetryContext<T>, CancellationToken, Task> onRetry);

        IRetryBuilder<T> HandleException<TEx>(Func<TEx, bool>? filter = null) 
            where  TEx : Exception;

        IRetryBuilder<T> Throw<TEx>(Func<TEx, bool>? filter = null)
            where TEx : Exception;

        IRetryBuilder<T> WithCancellation(CancellationToken cancellationToken);

        IRetryBuilder<T> ShouldSatisfyFor(uint times);

        IRetryBuilder<T> ShouldSatisfyFor(TimeSpan interval);

        IRetryBuilder<T> ReturnEvenFailed();
    }
}