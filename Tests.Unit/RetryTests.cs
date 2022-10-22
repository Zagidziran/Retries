using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Retries;
using Xunit;

namespace Tests.Unit
{
    public class RetryTests
    {
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
            invokeCount.Should().BeLessThan(6);
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
    }
}
