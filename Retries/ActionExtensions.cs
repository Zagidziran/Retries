using System.Linq.Expressions;

namespace Retries
{
    public static class ActionExtensions
    {
        public static IRetryBuilder<T> Retry<T>(this Func<CancellationToken, Task<T>> action)
        {
            return new RetryBuilder<T>(action);
        }
    }
}
