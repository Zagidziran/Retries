namespace Zagidziran.Retries
{
    public interface IRetryBuilder<T>
    {
        /// <summary>
        /// Defines a predicate to check a result of invoking action. 
        /// </summary>
        /// <param name="check">Predicate should return true if condition reached and retries can be stopped.</param>
        /// <returns>Self.</returns>
        IRetryBuilder<T> Until(Func<T, bool> check);

        /// <summary>
        /// Defines a timeout after which <see cref="TimeoutException"/> is thrown if condition defined in
        /// <see cref="Until"/> is not reached.
        /// </summary>
        /// <param name="timeout">Timeout value.</param>
        /// <returns>Self.</returns>
        IRetryBuilder<T> WithTimeout(TimeSpan timeout);

        /// <summary>
        /// Defines an interval between retries.
        /// </summary>
        /// <param name="interval">Interval value.</param>
        /// <returns>Self.</returns>
        IRetryBuilder<T> WithRetryInterval(TimeSpan interval);

        /// <summary>
        /// Defines a limit how many times the action can be retried. The resulting number of invocations can be
        /// greater by one. It is because first invocation is not retry.
        /// </summary>
        /// <param name="times">Retry number limit.</param>
        /// <returns>Self.</returns>
        IRetryBuilder<T> Times(uint times);

        /// <summary>
        /// Defines a method to be called before each retry.
        /// </summary>
        /// <param name="onRetry">A method to be called.</param>
        /// <returns>Self.</returns>
        IRetryBuilder<T> OnRetry(Func<RetryContext<T>, CancellationToken, Task> onRetry);

        /// <summary>
        /// Defines an exception type and optionally filter predicate to handle.
        /// It means exceptions passing the filter will be retried. Others will be thrown.
        /// </summary>
        /// <typeparam name="TEx">Type of exception to be handled with all descendants including.</typeparam>
        /// <param name="filter">A predicate should return true if exception should be handled.
        /// Exceptions of the proper type are handled without filtering if missing.</param>
        /// <returns>Self.</returns>
        IRetryBuilder<T> HandleException<TEx>(Func<TEx, bool>? filter = null) 
            where  TEx : Exception;

        /// <summary>
        /// The opposite of <see cref="HandleException{TEx}"/>. Defines a type of exception to throw
        /// even if it falls under <see cref="HandleException{TEx}"/> rules.
        /// </summary>
        /// <typeparam name="TEx">Type of exception to be thrown with all descendants including</typeparam>
        /// <param name="filter">A predicate should return true if exception should be throw.</param>
        /// <returns>Self.</returns>
        IRetryBuilder<T> Throw<TEx>(Func<TEx, bool>? filter = null)
            where TEx : Exception;

        /// <summary>
        /// Defines a cancellation token to abort retries.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token itself.</param>
        /// <returns>Self.</returns>
        IRetryBuilder<T> WithCancellation(CancellationToken cancellationToken);

        /// <summary>
        /// Defines a number of times the condition defined in the <see cref="Until"/> should be reached.
        /// It should helps to stabilize flickering result for example between different instances of the service.
        /// </summary>
        /// <param name="times">Number of times the condition should pass in a row between result returned.</param>
        /// <returns></returns>
        IRetryBuilder<T> ShouldSatisfyTimes(uint times);

        /// <summary>
        /// Defines a period of time when check defined in the <see cref="Until"/> should pass before result is returned.
        /// It is very similar to overload with number and can be combined with.
        /// </summary>
        /// <param name="interval">The interval during which condition should pass to complete retries.</param>
        /// <returns>Self.</returns>
        IRetryBuilder<T> ShouldSatisfyDuring(TimeSpan interval);

        /// <summary>
        /// Orders to return the result of the last successful attempt if present even if <see cref="Until"/> or
        /// <see cref="ShouldSatisfyFor"/> was not passed.
        /// </summary>
        /// <returns>Self.</returns>
        IRetryBuilder<T> ReturnEvenFailed();
    }
}