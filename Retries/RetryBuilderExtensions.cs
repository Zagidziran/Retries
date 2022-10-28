using System.Runtime.CompilerServices;

namespace Zagidziran.Retries
{
    public static class RetryBuilderExtensions
    {
        public static TaskAwaiter<T> GetAwaiter<T>(this IRetryBuilder<T> retryBuilder)
        {
            return ((RetryBuilder<T>) retryBuilder).GetAwaiter();
        }
    }
}
