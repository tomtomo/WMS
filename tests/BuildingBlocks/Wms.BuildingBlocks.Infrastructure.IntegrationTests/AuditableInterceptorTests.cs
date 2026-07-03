using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.BuildingBlocks.Infrastructure.IntegrationTests.TestSupport;
using Wms.BuildingBlocks.Infrastructure.Persistence;
using Xunit;

namespace Wms.BuildingBlocks.Infrastructure.IntegrationTests;

// Test AuditableInterceptor
[Collection(PostgresCollection.Name)]
public sealed class AuditableInterceptorTests(PostgresFixture postgres)
{
    private static readonly DateTimeOffset _createTime = new(2026, 7, 3, 8, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset _modifyTime = new(2026, 7, 3, 9, 30, 0, TimeSpan.Zero);

    [Fact]
    public async Task Fills_created_and_modified_from_the_current_user_on_insert()
    {
        var connectionString = await CreateSchemaAsync();
        var widget = new AuditableWidget { Id = Guid.NewGuid(), Name = "w" };

        await using var context = ContextFor(connectionString, AuthenticatedUser("alice"), _createTime);
        context.Set<AuditableWidget>().Add(widget);
        await context.SaveChangesAsync();

        widget.CreatedBy.Should().Be("alice");
        widget.ModifiedBy.Should().Be("alice");
        widget.CreatedAt.Should().Be(_createTime);
        widget.ModifiedAt.Should().Be(_createTime);
    }

    [Fact]
    public async Task Falls_back_to_system_actor_when_there_is_no_authenticated_user()
    {
        var connectionString = await CreateSchemaAsync();
        var widget = new AuditableWidget { Id = Guid.NewGuid(), Name = "w" };

        await using var context = ContextFor(connectionString, UnauthenticatedUser(), _createTime);
        context.Set<AuditableWidget>().Add(widget);
        await context.SaveChangesAsync();

        widget.CreatedBy.Should().Be("SYSTEM");
        widget.ModifiedBy.Should().Be("SYSTEM");
    }

    [Fact]
    public async Task Touches_only_modified_fields_on_update()
    {
        var connectionString = await CreateSchemaAsync();
        var id = Guid.NewGuid();

        await using (var insert = ContextFor(connectionString, AuthenticatedUser("alice"), _createTime))
        {
            insert.Set<AuditableWidget>().Add(new AuditableWidget { Id = id, Name = "w" });
            await insert.SaveChangesAsync();
        }

        await using var update = ContextFor(connectionString, AuthenticatedUser("bob"), _modifyTime);
        var widget = await update.Set<AuditableWidget>().SingleAsync(entity => entity.Id == id);
        widget.Name = "w-edited";
        await update.SaveChangesAsync();

        widget.CreatedBy.Should().Be("alice");
        widget.CreatedAt.Should().Be(_createTime);
        widget.ModifiedBy.Should().Be("bob");
        widget.ModifiedAt.Should().Be(_modifyTime);
    }

    [Fact]
    public async Task Leaves_non_auditable_entities_untouched()
    {
        var connectionString = await CreateSchemaAsync();
        var widget = new WidgetEntity { Id = Guid.NewGuid(), Name = "plain" };

        await using var context = ContextFor(connectionString, AuthenticatedUser("alice"), _createTime);
        context.Set<WidgetEntity>().Add(widget);
        var save = async () => await context.SaveChangesAsync();

        await save.Should().NotThrowAsync();
        widget.Name.Should().Be("plain");
    }

    private static ICurrentUser AuthenticatedUser(string userId)
    {
        var user = Substitute.For<ICurrentUser>();
        user.IsAuthenticated.Returns(true);
        user.UserId.Returns(userId);
        return user;
    }

    private static ICurrentUser UnauthenticatedUser()
    {
        var user = Substitute.For<ICurrentUser>();
        user.IsAuthenticated.Returns(false);
        return user;
    }

    private static RailTestDbContext ContextFor(string connectionString, ICurrentUser user, DateTimeOffset now) =>
        new(new DbContextOptionsBuilder<RailTestDbContext>()
            .UseNpgsql(connectionString)
            .AddInterceptors(new AuditableInterceptor(user, new FixedTimeProvider(now)))
            .Options);

    private async Task<string> CreateSchemaAsync()
    {
        var connectionString = await postgres.CreateFreshDatabaseAsync();
        await using var context = RailContext.New(connectionString);
        await context.Database.EnsureCreatedAsync();
        return connectionString;
    }
}
