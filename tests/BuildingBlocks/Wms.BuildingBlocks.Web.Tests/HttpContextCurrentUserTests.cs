using System.Security.Claims;
using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Wms.BuildingBlocks.Application.Abstractions;
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

    [Fact]
    public void Permissions_and_warehouses_are_hydrated_from_the_jwt_claims()
    {
        var w1 = Guid.NewGuid();
        var w2 = Guid.NewGuid();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(WmsClaimTypes.Subject, "user-1"),
                new Claim(WmsClaimTypes.Permission, "Inbound.PostGR"),
                new Claim(WmsClaimTypes.Permission, "Auth.ManageUser"),
                new Claim(WmsClaimTypes.Warehouse, w1.ToString()),
                new Claim(WmsClaimTypes.Warehouse, w2.ToString()),
            ],
            authenticationType: "jwt"));

        // HasPermission/CanBypassWarehouseScope hanya lewat tipe interface.
        ICurrentUser currentUser = new HttpContextCurrentUser(AccessorFor(new DefaultHttpContext { User = principal }));

        currentUser.Permissions.Should().HaveCount(2).And.Contain("Inbound.PostGR").And.Contain("Auth.ManageUser");
        currentUser.HasPermission("Inbound.PostGR").Should().BeTrue();
        currentUser.HasPermission("Inbound.ReadGR").Should().BeFalse();
        currentUser.AssignedWarehouseIds.Should().HaveCount(2).And.Contain(w1).And.Contain(w2);

        // User terautentikasi = terscope
        currentUser.CanBypassWarehouseScope.Should().BeFalse();
    }

    [Fact]
    public void An_unauthenticated_actor_has_no_permissions_and_bypasses_warehouse_scope()
    {
        ICurrentUser currentUser = new HttpContextCurrentUser(AccessorFor(new DefaultHttpContext()));

        currentUser.Permissions.Should().BeEmpty();
        currentUser.AssignedWarehouseIds.Should().BeEmpty();
        currentUser.HasPermission("Inbound.PostGR").Should().BeFalse();

        // SYSTEM/background/anonymous
        currentUser.CanBypassWarehouseScope.Should().BeTrue();
    }

    private static IHttpContextAccessor AccessorFor(HttpContext? httpContext)
    {
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpContext);
        return accessor;
    }
}
