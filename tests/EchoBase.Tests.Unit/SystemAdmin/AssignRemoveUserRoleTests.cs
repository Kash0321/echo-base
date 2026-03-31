using EchoBase.Core.Entities;
using EchoBase.Core.Interfaces;
using EchoBase.Core.SystemAdmin;
using EchoBase.Core.SystemAdmin.Commands;
using NSubstitute;

namespace EchoBase.Tests.Unit.SystemAdmin;

public class AssignRemoveUserRoleTests
{
    private static readonly Guid AdminId = Guid.NewGuid();
    private static readonly Guid TargetId = Guid.NewGuid();

    private readonly IBlockedDockRepository _blockedDockRepo = Substitute.For<IBlockedDockRepository>();
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly AssignUserRoleHandler _assignHandler;
    private readonly RemoveUserRoleHandler _removeHandler;

    public AssignRemoveUserRoleTests()
    {
        _assignHandler = new(_blockedDockRepo, _userRepo);
        _removeHandler = new(_blockedDockRepo, _userRepo);
    }

    private User BuildUser(bool hasManagerRole = false)
    {
        var user = new User(TargetId) { Name = "Test User", Email = "test@echo.com" };
        if (hasManagerRole)
        {
            var role = new Role(Guid.NewGuid()) { Name = "Manager" };
            user.Roles.Add(role);
        }
        return user;
    }

    private static Role BuildRole(string name) => new(Guid.NewGuid()) { Name = name };

    // ── AssignUserRole: Happy paths ───────────────────────────────

    [Theory]
    [InlineData("Manager")]
    [InlineData("SystemAdmin")]
    public async Task AssignRole_ValidRole_ReturnsSuccess(string roleName)
    {
        _blockedDockRepo.UserHasRoleAsync(AdminId, "SystemAdmin", Arg.Any<CancellationToken>()).Returns(true);
        var user = BuildUser();
        _userRepo.GetWithRolesAsync(TargetId, Arg.Any<CancellationToken>()).Returns(user);
        _userRepo.GetRoleByNameAsync(roleName, Arg.Any<CancellationToken>()).Returns(BuildRole(roleName));

        var result = await _assignHandler.Handle(
            new AssignUserRoleCommand(AdminId, TargetId, roleName), CancellationToken.None);

        Assert.True(result.IsSuccess);
        await _userRepo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AssignRole_RoleAddedToUser()
    {
        _blockedDockRepo.UserHasRoleAsync(AdminId, "SystemAdmin", Arg.Any<CancellationToken>()).Returns(true);
        var user = BuildUser();
        var role = BuildRole("Manager");
        _userRepo.GetWithRolesAsync(TargetId, Arg.Any<CancellationToken>()).Returns(user);
        _userRepo.GetRoleByNameAsync("Manager", Arg.Any<CancellationToken>()).Returns(role);

        await _assignHandler.Handle(
            new AssignUserRoleCommand(AdminId, TargetId, "Manager"), CancellationToken.None);

        Assert.Contains(role, user.Roles);
    }

    // ── AssignUserRole: Error paths ───────────────────────────────

    [Fact]
    public async Task AssignRole_NonSystemAdmin_ReturnsError()
    {
        _blockedDockRepo.UserHasRoleAsync(AdminId, "SystemAdmin", Arg.Any<CancellationToken>()).Returns(false);

        var result = await _assignHandler.Handle(
            new AssignUserRoleCommand(AdminId, TargetId, "Manager"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(SystemAdminErrors.NotSystemAdmin, result.Error);
    }

    [Theory]
    [InlineData("BasicUser")]
    [InlineData("SuperAdmin")]
    [InlineData("")]
    public async Task AssignRole_InvalidRole_ReturnsError(string roleName)
    {
        _blockedDockRepo.UserHasRoleAsync(AdminId, "SystemAdmin", Arg.Any<CancellationToken>()).Returns(true);

        var result = await _assignHandler.Handle(
            new AssignUserRoleCommand(AdminId, TargetId, roleName), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(SystemAdminErrors.InvalidRole, result.Error);
    }

    [Fact]
    public async Task AssignRole_UserNotFound_ReturnsError()
    {
        _blockedDockRepo.UserHasRoleAsync(AdminId, "SystemAdmin", Arg.Any<CancellationToken>()).Returns(true);
        _userRepo.GetWithRolesAsync(TargetId, Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await _assignHandler.Handle(
            new AssignUserRoleCommand(AdminId, TargetId, "Manager"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(SystemAdminErrors.UserNotFound, result.Error);
    }

    [Fact]
    public async Task AssignRole_RoleAlreadyAssigned_ReturnsError()
    {
        _blockedDockRepo.UserHasRoleAsync(AdminId, "SystemAdmin", Arg.Any<CancellationToken>()).Returns(true);
        var user = BuildUser(hasManagerRole: true);
        _userRepo.GetWithRolesAsync(TargetId, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _assignHandler.Handle(
            new AssignUserRoleCommand(AdminId, TargetId, "Manager"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(SystemAdminErrors.RoleAlreadyAssigned, result.Error);
    }

    // ── RemoveUserRole: Happy paths ───────────────────────────────

    [Fact]
    public async Task RemoveRole_ValidRole_ReturnsSuccess()
    {
        _blockedDockRepo.UserHasRoleAsync(AdminId, "SystemAdmin", Arg.Any<CancellationToken>()).Returns(true);
        var user = BuildUser(hasManagerRole: true);
        _userRepo.GetWithRolesAsync(TargetId, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _removeHandler.Handle(
            new RemoveUserRoleCommand(AdminId, TargetId, "Manager"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(user.Roles);
        await _userRepo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ── RemoveUserRole: Error paths ───────────────────────────────

    [Fact]
    public async Task RemoveRole_NonSystemAdmin_ReturnsError()
    {
        _blockedDockRepo.UserHasRoleAsync(AdminId, "SystemAdmin", Arg.Any<CancellationToken>()).Returns(false);

        var result = await _removeHandler.Handle(
            new RemoveUserRoleCommand(AdminId, TargetId, "Manager"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(SystemAdminErrors.NotSystemAdmin, result.Error);
    }

    [Fact]
    public async Task RemoveRole_UserNotFound_ReturnsError()
    {
        _blockedDockRepo.UserHasRoleAsync(AdminId, "SystemAdmin", Arg.Any<CancellationToken>()).Returns(true);
        _userRepo.GetWithRolesAsync(TargetId, Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await _removeHandler.Handle(
            new RemoveUserRoleCommand(AdminId, TargetId, "Manager"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(SystemAdminErrors.UserNotFound, result.Error);
    }

    [Fact]
    public async Task RemoveRole_RoleNotAssigned_ReturnsError()
    {
        _blockedDockRepo.UserHasRoleAsync(AdminId, "SystemAdmin", Arg.Any<CancellationToken>()).Returns(true);
        var user = BuildUser(hasManagerRole: false);
        _userRepo.GetWithRolesAsync(TargetId, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _removeHandler.Handle(
            new RemoveUserRoleCommand(AdminId, TargetId, "Manager"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(SystemAdminErrors.RoleNotAssigned, result.Error);
    }
}
