namespace Tests.Unit
{
    using FluentAssertions;
    using System.Threading.Tasks;
    using System.Threading;
    using Xunit;
    using Zagidziran.Retries;
    
    public class RetryBuilderExtensionsTests
    {
        [Fact]
        public async Task ShouldCorrectlyUseWrappedUntil()
        {
            // Arrange
            var invokeCount = 0;
            var responses = new[] { false, false, true };

            Task AnAction(CancellationToken token)
            {
                return Task.CompletedTask;
            }

            var action = AnAction;
            var retrieble = async () => await action
                .Retry()
                .Until(() => responses[invokeCount++]);

            // Act
            await retrieble();

            // Assert
            invokeCount.Should().Be(3);
        }
    }
}
