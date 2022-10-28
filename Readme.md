# Overture

This library provides methods to help organфize retires. But it is not polly competitor. There is another kind of retries when we waiting for condition.

### Premisses

Time to time pepole write tests. Time to time test turns into complicated process with a few components involved.
Lets say we have a database a syncroniztion logic and the functionality depending on the sync results.
And this functionality is being tested. So our flow is to put to database, wait synchronoztion, and only then examine our system under test.
The testing code have to wait until synchronization is performed with periodical checks. In more advanced situations becomes hard to organize such code.

### Solution

This library provides extension methods to decorate solutions of the problem described above. 

Like this:
```csharp
        [Fact]
        public async Task ShouldRelyOnRandom()
        {
            Task<byte> AnAction(CancellationToken token)
            {
                return Task.FromResult((byte)new Random().Next(100));
            }

            var method = AnAction;

            var result = await method
                .Retry()
                .Until(data => data == 7)
                .WithTimeout(TimeSpan.FromSeconds(10));
        }
```

# Usage

Basically, all the code is build on top of delegate extenison. This delegate accepts a canncellation token and returns a task. 
The signature looks so:
```csharp
public static IRetryBuilder<T> Retry<T>(this Func<CancellationToken, Task<T>> action);
```

Then all spins on the IRetryBuilder interface. It defines following methods wich should be used on a fluent manner: 

Method      | Description | Signature
------------|-------------|----------
Until       | Allows to specify a predicate to understand when we have to stop retry. | IRetryBuilder<T> Until(Func<T, bool> check)
WithTimeout | Allows to specify a time span after which TimeoutException is thrown when no other condition is reached. | IRetryBuilder<T> WithTimeout(TimeSpan timeout)
WithRetryInterval | Defines a time span to wait between retries. | IRetryBuilder<T> WithRetryInterval(TimeSpan interval)
Times | A number of attempts after which NoMoreRetriesException will be thrown if other conditions aren't reached. | IRetryBuilder<T> Times(uint times)
OnRetry | Defines a delegate to be called before retry. | IRetryBuilder<T> OnRetry(Func<RetryContext, CancellationToken, Task> onRetry)
HandleException | A way to specify exceptions to be retried. The parameter is delegate should return true to retry on exception and false to throw.  | IRetryBuilder<T> HandleException<TEx>(Func<TEx, bool>? filter = null) where  TEx : Exception
Throw | This method allows to define exceptions to throw even they pass HandleException conditions. Filter should return true to throw. | IRetryBuilder<T> Throw<TEx>(Func<TEx, bool>? filter = null) where TEx : Exception
WithCancellation | Defines a cancellation token to stop retries throw an OperationCancelledException when token is set. | IRetryBuilder<T> WithCancellation(CancellationToken cancellationToken)