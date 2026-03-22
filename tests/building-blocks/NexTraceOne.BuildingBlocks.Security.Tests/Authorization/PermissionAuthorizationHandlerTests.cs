using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Security.Authorization;
using NSubstitute;

namespace NexTraceOne.BuildingBlocks.Security.Tests.Authorization;

public sealed class PermissionAuthorizationHandlerTests
{
    private static (PermissionAuthorizationHandler Handler, ICurrentUser CurrentUser) CreateHandler(
        bool isAuthenticated = true,
        string userId = "user-1",
        IEnumerable<string>? grantedPermissions = null)
    {
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(isAuthenticated);
        currentUser.Id.Returns(userId);

        var permissions = grantedPermissions?.ToList() ?? [];
        currentUser.HasPermission(Arg.Any<string>())
            .Returns(callInfo => permissions.Contains(callInfo.Arg<string>(), StringComparer.OrdinalIgnoreCase));

        var logger = Substitute.For<ILogger<PermissionAuthorizationHandler>>();
        var handler = new PermissionAuthorizationHandler(currentUser, logger);

        return (handler, currentUser);
    }

    private static AuthorizationHandlerContext CreateContext(string permission, ClaimsPrincipal? user = null)
    {
        var requirement = new PermissionRequirement(permission);
        user ??= new ClaimsPrincipal(new ClaimsIdentity([], "Test"));
        return new AuthorizationHandlerContext([requirement], user, null);
    }

    [Fact]
    public async Task HandleRequirement_WithCorrectPermission_Succeeds()
    {
        var (handler, _) = CreateHandler(grantedPermissions: ["services:read"]);
        var context = CreateContext("services:read");

        await ((IAuthorizationHandler)handler).HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task HandleRequirement_WithoutRequiredPermission_DoesNotSucceed()
    {
        var (handler, _) = CreateHandler(grantedPermissions: ["services:read"]);
        var context = CreateContext("services:write");

        await ((IAuthorizationHandler)handler).HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task HandleRequirement_WithUnauthenticatedUser_DoesNotSucceed()
    {
        var (handler, _) = CreateHandler(isAuthenticated: false);
        var context = CreateContext("services:read");

        await ((IAuthorizationHandler)handler).HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task HandleRequirement_WithEmptyClaims_DoesNotSucceed()
    {
        var (handler, _) = CreateHandler(grantedPermissions: []);
        var context = CreateContext("services:read");

        await ((IAuthorizationHandler)handler).HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task HandleRequirement_MultiplePermissions_OnlyMatchingSucceeds()
    {
        var (handler, _) = CreateHandler(grantedPermissions: ["services:read", "identity:users:write"]);

        var readContext = CreateContext("services:read");
        await ((IAuthorizationHandler)handler).HandleAsync(readContext);
        readContext.HasSucceeded.Should().BeTrue();

        var deleteContext = CreateContext("services:delete");
        await ((IAuthorizationHandler)handler).HandleAsync(deleteContext);
        deleteContext.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task HandleRequirement_AuthenticatedWithoutPermission_LogsWarning()
    {
        var logger = Substitute.For<ILogger<PermissionAuthorizationHandler>>();
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.Id.Returns("user-42");
        currentUser.HasPermission(Arg.Any<string>()).Returns(false);

        var handler = new PermissionAuthorizationHandler(currentUser, logger);
        var context = CreateContext("admin:delete");

        await ((IAuthorizationHandler)handler).HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
        logger.ReceivedCalls().Should().NotBeEmpty();
    }
}
