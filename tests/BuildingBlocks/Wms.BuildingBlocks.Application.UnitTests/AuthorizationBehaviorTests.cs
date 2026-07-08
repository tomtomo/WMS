using AwesomeAssertions;
using MediatR;
using Wms.BuildingBlocks.Application.Abstractions;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.BuildingBlocks.Application.Behaviors;
using Wms.BuildingBlocks.Domain.Results;
using Xunit;

namespace Wms.BuildingBlocks.Application.UnitTests;

// Memastikan AuthorizationBehavior hanya mengizinkan request yang punya permission.
public sealed class AuthorizationBehaviorTests
{
    private const string RequiredPermission = "Test.Manage";

    [Fact]
    public async Task Request_without_marker_passes_through()
    {
        var handlerCalled = false;
        var behavior = new AuthorizationBehavior<OpenRequest, Result<int>>(new TestUser(isAuthenticated: true));

        var result = await behavior.Handle(new OpenRequest(), Next(() => handlerCalled = true), CancellationToken.None);

        handlerCalled.Should().BeTrue();
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Authenticated_user_with_permission_reaches_handler()
    {
        var handlerCalled = false;
        var behavior = new AuthorizationBehavior<SecuredRequest, Result<int>>(
            new TestUser(isAuthenticated: true, RequiredPermission));

        var result = await behavior.Handle(new SecuredRequest(), Next(() => handlerCalled = true), CancellationToken.None);

        handlerCalled.Should().BeTrue();
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Authenticated_user_without_permission_is_forbidden_and_handler_untouched()
    {
        var handlerCalled = false;
        var behavior = new AuthorizationBehavior<SecuredRequest, Result<int>>(new TestUser(isAuthenticated: true));

        var result = await behavior.Handle(new SecuredRequest(), Next(() => handlerCalled = true), CancellationToken.None);

        handlerCalled.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ResultErrorType.Forbidden);
        result.Error.Code.Should().Be("authorization.forbidden");
    }

    [Fact]
    public async Task System_actor_bypasses_permission_check()
    {
        var handlerCalled = false;
        var behavior = new AuthorizationBehavior<SecuredRequest, Result<int>>(new TestUser(isAuthenticated: false));

        var result = await behavior.Handle(new SecuredRequest(), Next(() => handlerCalled = true), CancellationToken.None);

        handlerCalled.Should().BeTrue();
        result.IsSuccess.Should().BeTrue();
    }

    private static RequestHandlerDelegate<Result<int>> Next(Action onCalled) =>
        _ =>
        {
            onCalled();
            return Task.FromResult(Result.Success(1));
        };

    [RequiresPermission(RequiredPermission)]
    private sealed record SecuredRequest : IRequest<Result<int>>;

    private sealed record OpenRequest : IRequest<Result<int>>;

    private sealed class TestUser(bool isAuthenticated, params string[] permissions) : ICurrentUser
    {
        public string UserId => "test-user";

        public bool IsAuthenticated => isAuthenticated;

        public IReadOnlyCollection<string> Permissions => permissions;
    }
}
