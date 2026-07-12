using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Wms.BuildingBlocks.Infrastructure.DeadLetter;
using Wms.BuildingBlocks.Infrastructure.IntegrationTests.TestSupport;
using Xunit;

namespace Wms.BuildingBlocks.Infrastructure.IntegrationTests;

// Test DLQ consumer
[Collection(PostgresCollection.Name)]
public sealed class DeadLetterPipelineTests(PostgresFixture postgres)
{
    [Fact]
    public async Task Writes_one_dead_letter_row_and_logs_a_warning_after_the_retry_budget_is_exhausted()
    {
        await using var context = await NewContextAsync();
        var logger = new RecordingLogger();
        var pipeline = new ConsumerDeadLetterPipeline(
            new DeadLetterStore(context, TimeProvider.System),
            TimeProvider.System,
            logger,
            TimeSpan.Zero);

        var attempts = 0;
        await pipeline.ExecuteAsync(
            "inbound.gr_confirmed.v1",
            "{\"id\":1}",
            _ =>
            {
                attempts++;
                throw new InvalidOperationException("handler boom");
            });

        attempts.Should().Be(ConsumerDeadLetterPipeline.MaxAttempts);
        var deadLetters = await context.Set<DeadLetterRecord>().ToListAsync();
        deadLetters.Should().ContainSingle();
        deadLetters[0].Source.Should().Be("inbound.gr_confirmed.v1");
        deadLetters[0].Error.Should().Contain("handler boom");
        deadLetters[0].Payload.Should().Be("{\"id\":1}");
        deadLetters[0].AttemptCount.Should().Be(ConsumerDeadLetterPipeline.MaxAttempts);

        // Pastikan kegagalan yang masuk dead-letter tetap tercatat di log agar mudah dilacak.
        logger.Warnings.Should().ContainSingle()
            .Which.Should().Contain("inbound.gr_confirmed.v1").And.Contain("handler boom");
    }

    [Fact]
    public async Task Does_not_dead_letter_when_the_handler_eventually_succeeds()
    {
        await using var context = await NewContextAsync();
        var logger = new RecordingLogger();
        var pipeline = new ConsumerDeadLetterPipeline(
            new DeadLetterStore(context, TimeProvider.System),
            TimeProvider.System,
            logger,
            TimeSpan.Zero);

        var attempts = 0;
        await pipeline.ExecuteAsync(
            "inbound.gr_confirmed.v1",
            "{}",
            _ =>
            {
                attempts++;
                if (attempts < 2)
                {
                    throw new InvalidOperationException("transient");
                }

                return Task.CompletedTask;
            });

        attempts.Should().Be(2);
        (await context.Set<DeadLetterRecord>().CountAsync()).Should().Be(0);
        logger.Warnings.Should().BeEmpty();
    }

    private async Task<RailTestDbContext> NewContextAsync()
    {
        var connectionString = await postgres.CreateFreshDatabaseAsync();
        var context = RailContext.New(connectionString);
        await context.Database.EnsureCreatedAsync();
        return context;
    }

    // Logger sederhana untuk merekam warning tanpa full logging test.
    private sealed class RecordingLogger : ILogger<ConsumerDeadLetterPipeline>
    {
        public List<string> Warnings { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Warning)
            {
                Warnings.Add(formatter(state, exception));
            }
        }
    }
}
