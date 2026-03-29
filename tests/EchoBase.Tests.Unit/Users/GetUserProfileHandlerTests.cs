using EchoBase.Core.Entities.Enums;
using EchoBase.Core.Interfaces;
using EchoBase.Core.Users;
using EchoBase.Core.Users.Queries;
using NSubstitute;

namespace EchoBase.Tests.Unit.Users;

public class GetUserProfileHandlerTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    private readonly IUserRepository _repository = Substitute.For<IUserRepository>();
    private readonly GetUserProfileHandler _handler;

    public GetUserProfileHandlerTests()
    {
        _handler = new(_repository);
    }

    private static UserProfileDto MakeProfile(Guid? id = null) => new(
        id ?? UserId,
        "Leia Organa",
        "leia@rebelbase.com",
        BusinessLine.Core,
        "+34 600 111 222",
        EmailNotifications: true,
        TeamsNotifications: false);

    private GetUserProfileQuery Query(Guid? userId = null) => new(userId ?? UserId);

    // ──────────────────────────────────────────────────────────────
    // Happy path
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ExistingUser_ReturnsProfile()
    {
        var profile = MakeProfile();
        _repository.GetProfileAsync(UserId, Arg.Any<CancellationToken>()).Returns(profile);

        var result = await _handler.Handle(Query(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(UserId, result.Value.Id);
        Assert.Equal("Leia Organa", result.Value.Name);
        Assert.Equal("leia@rebelbase.com", result.Value.Email);
        Assert.Equal(BusinessLine.Core, result.Value.BusinessLine);
        Assert.Equal("+34 600 111 222", result.Value.PhoneNumber);
        Assert.True(result.Value.EmailNotifications);
        Assert.False(result.Value.TeamsNotifications);
    }

    [Fact]
    public async Task Handle_ExistingUser_WithNullPhone_ReturnsProfile()
    {
        var profile = MakeProfile() with { PhoneNumber = null };
        _repository.GetProfileAsync(UserId, Arg.Any<CancellationToken>()).Returns(profile);

        var result = await _handler.Handle(Query(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value!.PhoneNumber);
    }

    // ──────────────────────────────────────────────────────────────
    // Error paths
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_UserNotFound_ReturnsFailure()
    {
        _repository.GetProfileAsync(UserId, Arg.Any<CancellationToken>()).Returns((UserProfileDto?)null);

        var result = await _handler.Handle(Query(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(UserErrors.UserNotFound, result.Error);
    }
}
