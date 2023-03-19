namespace Zagidziran.Retries
{
    public static class ActionExtensions
    {
        public static IRetryBuilder<T> Retry<T>(this Func<CancellationToken, Task<T>> action)
        {
            return new RetryBuilder<T>(action);
        }

        public static IRetryBuilder<object?> Retry(this Func<CancellationToken, Task> action)
        {
            return new RetryBuilder<object?>(async ct =>
            {
                await action(ct);
                return null;
            });
        }
    }
}
