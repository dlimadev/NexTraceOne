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
        IEnumerable<string>? grantedPermissions = null,
        IDatabasePermissionProvider? dbProvider = null,
        IModuleAccessPermissionProvider? moduleAccessProvider = null,
        IJitPermissionProvider? jitProvider = null)
    {
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(isAuthenticated);
        currentUser.Id.Returns(userId);

        var permissions = grantedPermissions?.ToList() ?? [];
        currentUser.HasPermission(Arg.Any<string>())
            .Returns(callInfo => permissions.Contains(callInfo.Arg<string>(), StringComparer.OrdinalIgnoreCase));

        var logger = Substitute.For<ILogger<PermissionAuthorizationHandler>>();
        var handler = new PermissionAuthorizationHandler(currentUser, logger, dbProvider, moduleAccessProvider, jitProvider);

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

    // ── Testes da cascata: JWT → Database → ModuleAccess → JIT ───────────

    [Fact]
    public async Task HandleRequirement_WithModuleAccessPolicy_Succeeds_When_JwtAndDbDoNotHaveIt()
    {
        var dbProvider = Substitute.For<IDatabasePermissionProvider>();
        dbProvider.HasPermissionAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var moduleAccessProvider = Substitute.For<IModuleAccessPermissionProvider>();
        moduleAccessProvider.HasModuleAccessAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            "ai:runtime:write", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<bool?>(true));

        var (handler, _) = CreateHandler(
            grantedPermissions: [],
            dbProvider: dbProvider,
            moduleAccessProvider: moduleAccessProvider);

        var context = CreateContext("ai:runtime:write");
        await ((IAuthorizationHandler)handler).HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task HandleRequirement_WithModuleAccessDeny_DeniesAccess_Even_When_JitWouldAllow()
    {
        var dbProvider = Substitute.For<IDatabasePermissionProvider>();
        dbProvider.HasPermissionAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var moduleAccessProvider = Substitute.For<IModuleAccessPermissionProvider>();
        moduleAccessProvider.HasModuleAccessAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            "governance:admin:write", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<bool?>(false));

        var jitProvider = Substitute.For<IJitPermissionProvider>();
        jitProvider.HasActiveJitGrantAsync("user-1", "governance:admin:write", Arg.Any<CancellationToken>())
            .Returns(true);

        var (handler, _) = CreateHandler(
            grantedPermissions: [],
            dbProvider: dbProvider,
            moduleAccessProvider: moduleAccessProvider,
            jitProvider: jitProvider);

        var context = CreateContext("governance:admin:write");
        await ((IAuthorizationHandler)handler).HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
        await jitProvider.DidNotReceive().HasActiveJitGrantAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleRequirement_ModuleAccessNull_FallsThrough_ToJit()
    {
        var dbProvider = Substitute.For<IDatabasePermissionProvider>();
        dbProvider.HasPermissionAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var moduleAccessProvider = Substitute.For<IModuleAccessPermissionProvider>();
        moduleAccessProvider.HasModuleAccessAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<bool?>(null));

        var jitProvider = Substitute.For<IJitPermissionProvider>();
        jitProvider.HasActiveJitGrantAsync("user-1", "platform:admin:read", Arg.Any<CancellationToken>())
            .Returns(true);

        var (handler, _) = CreateHandler(
            grantedPermissions: [],
            dbProvider: dbProvider,
            moduleAccessProvider: moduleAccessProvider,
            jitProvider: jitProvider);

        var context = CreateContext("platform:admin:read");
        await ((IAuthorizationHandler)handler).HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task HandleRequirement_WithDbPermission_Succeeds_When_JwtDoesNotHaveIt()
    {
        var dbProvider = Substitute.For<IDatabasePermissionProvider>();
        dbProvider.HasPermissionAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            "governance:admin:write", Arg.Any<CancellationToken>())
            .Returns(true);

        var (handler, _) = CreateHandler(
            grantedPermissions: [],
            dbProvider: dbProvider);

        var context = CreateContext("governance:admin:write");
        await ((IAuthorizationHandler)handler).HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task HandleRequirement_WithJitGrant_Succeeds_When_JwtAndDbDoNotHaveIt()
    {
        var dbProvider = Substitute.For<IDatabasePermissionProvider>();
        dbProvider.HasPermissionAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var jitProvider = Substitute.For<IJitPermissionProvider>();
        jitProvider.HasActiveJitGrantAsync("user-1", "platform:admin:read", Arg.Any<CancellationToken>())
            .Returns(true);

        var (handler, _) = CreateHandler(
            grantedPermissions: [],
            dbProvider: dbProvider,
            jitProvider: jitProvider);

        var context = CreateContext("platform:admin:read");
        await ((IAuthorizationHandler)handler).HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task HandleRequirement_JwtTakesPrecedence_When_JwtHasPermission()
    {
        var dbProvider = Substitute.For<IDatabasePermissionProvider>();
        var jitProvider = Substitute.For<IJitPermissionProvider>();

        var (handler, _) = CreateHandler(
            grantedPermissions: ["services:read"],
            dbProvider: dbProvider,
            jitProvider: jitProvider);

        var context = CreateContext("services:read");
        await ((IAuthorizationHandler)handler).HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();

        // DB and JIT should NOT have been called since JWT had the permission
        await dbProvider.DidNotReceive().HasPermissionAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<CancellationToken>());
        await jitProvider.DidNotReceive().HasActiveJitGrantAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleRequirement_DeniesAccess_When_AllProvidersReturnFalse()
    {
        var dbProvider = Substitute.For<IDatabasePermissionProvider>();
        dbProvider.HasPermissionAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var jitProvider = Substitute.For<IJitPermissionProvider>();
        jitProvider.HasActiveJitGrantAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var (handler, _) = CreateHandler(
            grantedPermissions: [],
            dbProvider: dbProvider,
            jitProvider: jitProvider);

        var context = CreateContext("nonexistent:permission");
        await ((IAuthorizationHandler)handler).HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }
}
