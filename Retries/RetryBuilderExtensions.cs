using System.Runtime.CompilerServices;

namespace Retries
{
    public static class RetryBuilderExtensions
    {
        public static TaskAwaiter<T> GetAwaiter<T>(this IRetryBuilder<T> retryBuilder)
        {
            return ((RetryBuilder<T>) retryBuilder).GetAwaiter();
        }
    }
}
