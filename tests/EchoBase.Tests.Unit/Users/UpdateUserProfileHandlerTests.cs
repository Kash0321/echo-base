using EchoBase.Core.Entities;
using EchoBase.Core.Entities.Enums;
using EchoBase.Core.Interfaces;
using EchoBase.Core.Users;
using EchoBase.Core.Users.Commands;
using NSubstitute;

namespace EchoBase.Tests.Unit.Users;

public class UpdateUserProfileHandlerTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    private readonly IUserRepository _repository = Substitute.For<IUserRepository>();
    private readonly UpdateUserProfileHandler _handler;

    public UpdateUserProfileHandlerTests()
    {
        _handler = new(_repository);
    }

    private static User MakeUser() =>
        new(UserId) { Name = "Han Solo", Email = "han@rebelbase.com" };

    private UpdateUserProfileCommand Cmd(
        BusinessLine businessLine = BusinessLine.Core,
        string? phoneNumber = "+34 600 000 000",
        bool emailNotifications = true,
        bool teamsNotifications = false) =>
        new(UserId, businessLine, phoneNumber, emailNotifications, teamsNotifications);

    // ──────────────────────────────────────────────────────────────
    // Happy path
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ValidRequest_UpdatesProfileAndSaves()
    {
        var user = MakeUser();
        _repository.GetForUpdateAsync(UserId, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _handler.Handle(Cmd(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(BusinessLine.Core, user.BusinessLine);
        Assert.Equal("+34 600 000 000", user.PhoneNumber);
        Assert.True(user.EmailNotifications);
        Assert.False(user.TeamsNotifications);
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NullPhone_ClearsPhoneNumber()
    {
        var user = MakeUser();
        _repository.GetForUpdateAsync(UserId, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _handler.Handle(Cmd(phoneNumber: null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(user.PhoneNumber);
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhitespacePhone_TreatsAsNull()
    {
        var user = MakeUser();
        _repository.GetForUpdateAsync(UserId, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _handler.Handle(Cmd(phoneNumber: "   "), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(user.PhoneNumber);
    }

    [Fact]
    public async Task Handle_TeamsNotificationsEnabled_UpdatesFlag()
    {
        var user = MakeUser();
        _repository.GetForUpdateAsync(UserId, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _handler.Handle(
            Cmd(emailNotifications: false, teamsNotifications: true), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(user.EmailNotifications);
        Assert.True(user.TeamsNotifications);
    }

    // ──────────────────────────────────────────────────────────────
    // Validation errors
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_PhoneNumberTooLong_ReturnsFailure()
    {
        var longPhone = new string('9', 31);

        var result = await _handler.Handle(Cmd(phoneNumber: longPhone), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(UserErrors.PhoneNumberTooLong, result.Error);
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UserNotFound_ReturnsFailure()
    {
        _repository.GetForUpdateAsync(UserId, Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await _handler.Handle(Cmd(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(UserErrors.UserNotFound, result.Error);
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ExactMaxLengthPhone_Succeeds()
    {
        var user = MakeUser();
        _repository.GetForUpdateAsync(UserId, Arg.Any<CancellationToken>()).Returns(user);
        var maxPhone = new string('9', 30);

        var result = await _handler.Handle(Cmd(phoneNumber: maxPhone), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(maxPhone, user.PhoneNumber);
    }
}
