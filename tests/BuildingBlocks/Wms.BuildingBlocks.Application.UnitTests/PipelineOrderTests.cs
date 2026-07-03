using AwesomeAssertions;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Wms.BuildingBlocks.Application.Abstractions;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.BuildingBlocks.Application.UnitTests.TestDoubles;
using Wms.BuildingBlocks.Domain.Results;
using Xunit;

namespace Wms.BuildingBlocks.Application.UnitTests;

// Test urutan pipeline
public sealed class PipelineOrderTests
{
    [Fact]
    public async Task Behaviors_execute_in_the_locked_entry_order()
    {
        var recorder = new PipelineRecorder();
        var services = new ServiceCollection();
        services.AddSingleton(recorder);
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Trace);
            builder.AddProvider(new RecordingLoggerProvider(recorder));
        });
        services.AddApplicationBuildingBlocks(typeof(PipelineOrderTests).Assembly);

        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                recorder.Add("Transaction");
                return Result.Success();
            });
        services.AddSingleton(unitOfWork);

        var auditLogStore = Substitute.For<IAuditLogStore>();
        auditLogStore
            .When(store => store.RecordAsync(Arg.Any<AuditLogEntry>(), Arg.Any<CancellationToken>()))
            .Do(_ => recorder.Add("AuditLog"));
        services.AddSingleton(auditLogStore);

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns("tester");
        services.AddSingleton(currentUser);

        await using var provider = services.BuildServiceProvider();
        var result = await provider.GetRequiredService<IMediator>().Send(new RecordingCommand(1));

        result.IsSuccess.Should().BeTrue();

        recorder.Steps.Should().Equal("Validation", "Handler", "Logging", "AuditLog", "Transaction");
    }

    private sealed class RecordingLoggerProvider(PipelineRecorder recorder) : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) =>
            categoryName.Contains("LoggingBehavior", StringComparison.Ordinal)
                ? new RecordingLogger(recorder)
                : NullLogger.Instance;

        public void Dispose()
        {
            // Tak ada resource yang perlu dilepas.
        }

        private sealed class RecordingLogger(PipelineRecorder recorder) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state)
                where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
                => recorder.Add("Logging");
        }
    }
}
