using System.Security.Claims;
using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.BuildingBlocks.Web.Auth;
using Xunit;

namespace Wms.BuildingBlocks.Web.Tests;

public sealed class HttpContextCurrentUserTests
{
    [Fact]
    public void UserId_is_the_subject_claim_when_principal_is_authenticated()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, "user-42")],
            authenticationType: "jwt"));
        var currentUser = new HttpContextCurrentUser(AccessorFor(new DefaultHttpContext { User = principal }));

        currentUser.IsAuthenticated.Should().BeTrue();
        currentUser.UserId.Should().Be("user-42");
    }

    [Fact]
    public void UserId_falls_back_to_SYSTEM_when_principal_is_unauthenticated()
    {
        var currentUser = new HttpContextCurrentUser(AccessorFor(new DefaultHttpContext()));

        currentUser.IsAuthenticated.Should().BeFalse();
        currentUser.UserId.Should().Be(ICurrentUser.SystemActor);
    }

    [Fact]
    public void UserId_falls_back_to_SYSTEM_when_there_is_no_http_context()
    {
        var currentUser = new HttpContextCurrentUser(AccessorFor(httpContext: null));

        currentUser.IsAuthenticated.Should().BeFalse();
        currentUser.UserId.Should().Be(ICurrentUser.SystemActor);
    }

    private static IHttpContextAccessor AccessorFor(HttpContext? httpContext)
    {
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpContext);
        return accessor;
    }
}
