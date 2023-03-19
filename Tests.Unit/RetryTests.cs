using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using Zagidziran.Retries;

namespace Tests.Unit
{
    public class RetryTests
    {

        [Fact]
        public async Task ShouldReturnEvenError()
        {
            // Arrange
            var invokeCount = 0;

            Task<bool> AnAction(CancellationToken token)
            {
                if (invokeCount++ == 1)
                {
                    throw new Exception();
                }

                return Task.FromResult(true);
            }

            var action = AnAction;

            // Act
            var result = await action
                .Retry()
                .HandleException<Exception>()
                .Times(5)
                .ShouldSatisfyTimes(3)
                .ReturnEvenFailed();

            // Assert
            result.Should().BeTrue();

        }

        [Theory]
        [InlineData(1, 100)]
        [InlineData(5, 0)]
        public async Task ShouldRespectBothSatisfactionCountAndRetries(uint times, int satisfactionPeriod)
        {
            // Arrange
            var invokeCount = 0;

            Task<bool> AnAction(CancellationToken token)
            {
                return Task.FromResult(invokeCount++ >= 3);
            }

            var action = AnAction;
            var retrieble = async () => await action
                .Retry()
                .Until(yeah => yeah!)
                .WithRetryInterval(TimeSpan.FromMilliseconds(15))
                .ShouldSatisfyTimes(times)
                .ShouldSatisfyDuring(TimeSpan.FromMilliseconds(satisfactionPeriod));

            // Act & Assert
            await retrieble.Should().NotThrowAsync();
            // At least 5 satisfy probes expected
            invokeCount.Should().BeGreaterThan(6);
        }


        [Fact]
        public async Task ShouldCountSatisfactionFailAsSingleRetry()
        {
            // Arrange
            var invokeCount = 0;
            var responses = new[] { false, false, true, true, false, true, true, true };

            Task<bool> AnAction(CancellationToken token)
            {
                return Task.FromResult(responses[invokeCount++]);
            }

            var action = AnAction;
            var retrieble = async () => await action
                .Retry()
                .Until(yeah => yeah!)
                .WithRetryInterval(TimeSpan.FromMilliseconds(20))
                .Times(5)
                .ShouldSatisfyTimes(3);

            // Act & Assert
            await retrieble.Should().NotThrowAsync();
        }

        [Fact]
        public async Task ShouldCheckSatisfactionTimes()
        {
            // Arrange
            var invokeCount = 0;

            Task<bool> AnAction(CancellationToken token)
            {
                invokeCount++;
                if (invokeCount <= 3)
                {
                    throw new Exception();
                }

                return Task.FromResult(true);
            }

            var action = AnAction;
            var retrieble = action
                .Retry()
                .HandleException<Exception>()
                .WithRetryInterval(TimeSpan.FromMilliseconds(20))
                .Times(3)
                .ShouldSatisfyTimes(5);

            // Act
            await retrieble;

            // Assert
            invokeCount.Should().Be(8);
        }

        [Fact]
        public async Task ShouldCheckSatisfactionPeriod()
        {
            // Arrange
            var invokeCount = 0;
            
            Task<bool> AnAction(CancellationToken token)
            {
                invokeCount++;
                if (invokeCount < 3)
                {
                    throw new Exception();
                }

                return Task.FromResult(true);
            }

            var action = AnAction;
            var retrieble = action
                .Retry()
                .HandleException<Exception>()
                .WithRetryInterval(TimeSpan.FromMilliseconds(20))
                .Times(3)
                .ShouldSatisfyDuring(TimeSpan.FromMilliseconds(200));

            // Act
            await retrieble;

            // Assert
            invokeCount.Should().BeGreaterThan(5);
        }

        [Fact]
        public async Task ShouldRetry()
        {
            // Arrange
            var invokeCount = 0;
            Task<bool> AnAction(CancellationToken token)
            {
                invokeCount++;
                throw new Exception();
            }

            var action = AnAction;
            var retrieble = async () => await action
                 .Retry()
                 .HandleException<Exception>()
                 .Times(3);
            // Act
            await retrieble.Should().ThrowAsync<Exception>();

            // Assert
            invokeCount.Should().Be(4);
        }

        [Fact]
        public async Task ShouldRetryUntil()
        {
            // Arrange
            var invokeCount = 0;
            Task<bool> AnAction(CancellationToken token)
            {
                invokeCount++;
                return Task.FromResult(true);
            }

            var action = AnAction;

            // Act
            await action
                .Retry()
                .HandleException<Exception>()
                .Until(_ => invokeCount == 3);

            // Assert
            invokeCount.Should().Be(3);
        }

        [Fact]
        public async Task ShouldTimeOut()
        {
            // Arrange
            Task<bool> AnAction(CancellationToken token)
            {
                return Task.FromResult(false);
            }

            var action = AnAction;

            // Act
            var retrieble = async () => await action
                .Retry()
                .Until(_ => false)
                .WithTimeout(TimeSpan.FromMilliseconds(20));

            // Assert
            await retrieble.Should().ThrowAsync<TimeoutException>();
        }

        [Fact]
        public async Task ShouldRespectCancellationToken()
        {
            // Arrange
            var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(10));
            Task<bool> AnAction(CancellationToken token)
            {
                return Task.FromResult(false);
            }

            var action = AnAction;

            // Act
            var retrieble = async () => await action
                .Retry()
                .Until(_ => false)
                .WithCancellation(cts.Token);

            // Assert
            await retrieble.Should().ThrowAsync<OperationCanceledException>();
        }

        [Fact]
        public async Task ShouldNotHandleException()
        {
            // Arrange
            var exceptionIndex = 0;
            var exceptions = new[]
            {
                new Exception(),
                new Exception(),
                new ApplicationException(),
            };

            Task<bool> AnAction(CancellationToken token)
            {
                throw exceptions[exceptionIndex++];
            }

            var action = AnAction;
            var retrieble = async () => await action
                .Retry()
                .Until(_ => false)
                .HandleException<Exception>()
                .Throw<ApplicationException>();

            await retrieble.Should().ThrowAsync<ApplicationException>();
        }

        [Fact]
        public async Task ShouldMaintainRetryInterval()
        {
            // Arrange
            var invokeCount = 0;

            Task<bool> AnAction(CancellationToken token)
            {
                invokeCount++;
                throw new Exception();
            }

            var action = AnAction;

            // Act
            var retrieble = async () => await action
                .Retry()
                .Until(_ => false)
                .HandleException<Exception>()
                .WithTimeout(TimeSpan.FromMilliseconds(50))
                .WithRetryInterval(TimeSpan.FromMilliseconds(10));

            // Arrange
            await retrieble.Should().ThrowAsync<Exception>();
            //// As far as delays is not completely reliable we only can expect we won't exceed a retries number
            invokeCount.Should().BeLessThan(7);
        }

        [Fact]
        public async Task ShouldCallOnRetry()
        {
            // Arrange
            var onRetryInvoked = false;
            Task<bool> AnAction(CancellationToken token)
            {
                return Task.FromResult(false);
            }

            var action = AnAction;
            
            // Act
            await action
                .Retry()
                .OnRetry((_, _) =>
                {
                    onRetryInvoked = true;
                    return Task.CompletedTask;
                })
                .Until(_ => onRetryInvoked);

            // Assert
            onRetryInvoked.Should().BeTrue();
        }

        [Fact]
        public async Task ShouldRunActionTwice()
        {
            // Arrange
            var invokeCount = 0;
            Task<bool> AnAction(CancellationToken token)
            {
                invokeCount++;
                return Task.FromResult(false);
            }

            var action = AnAction;

            var retrier = action
                .Retry()
                .Until(_ => true);

            // Act
            await retrier;
            await retrier;

            // Assert
            invokeCount.Should().Be(2);
        }

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
    }
}
