using EchoBase.Core.Common;
using EchoBase.Core.Entities;
using EchoBase.Core.Entities.Enums;
using EchoBase.Core.Interfaces;
using MediatR;
using NSubstitute;

namespace EchoBase.Tests.Unit.SystemAdmin;

public class AuditLoggingBehaviorTests
{
    private readonly IAuditLogRepository _auditLogRepo = Substitute.For<IAuditLogRepository>();
    private readonly TimeProvider _time = Substitute.For<TimeProvider>();
    private readonly AuditLoggingBehavior<TestAuditableCommand, Result> _behavior;
    private readonly AuditLoggingBehavior<TestNonAuditableCommand, Result> _behaviorNonAuditable;
    private readonly AuditLoggingBehavior<TestAuditableCommandWithGuidResult, Result<Guid>> _behaviorGenericResult;

    public AuditLoggingBehaviorTests()
    {
        _time.GetUtcNow().Returns(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));
        _behavior = new(_auditLogRepo, _time);
        _behaviorNonAuditable = new(_auditLogRepo, _time);
        _behaviorGenericResult = new(_auditLogRepo, _time);    }

    // ── Auditable + Success → should log ─────────────────────────

    [Fact]
    public async Task Handle_AuditableCommandSuccess_WritesAuditLog()
    {
        var cmd = new TestAuditableCommand();
        RequestHandlerDelegate<Result> next = () => Task.FromResult(Result.Success());

        await _behavior.Handle(cmd, next, CancellationToken.None);

        await _auditLogRepo.Received(1).AddAsync(
            Arg.Is<AuditLog>(l =>
                l.Action == AuditAction.MaintenanceModeChanged &&
                l.PerformedByUserId == cmd.PerformedByUserId &&
                l.Details == cmd.BuildAuditDetails()),
            Arg.Any<CancellationToken>());
        await _auditLogRepo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AuditableCommandSuccessResultT_WritesAuditLog()
    {
        var cmd = new TestAuditableCommandWithGuidResult();
        RequestHandlerDelegate<Result<Guid>> next = () => Task.FromResult(Result<Guid>.Success(Guid.NewGuid()));

        await _behaviorGenericResult.Handle(cmd, next, CancellationToken.None);

        await _auditLogRepo.Received(1).AddAsync(Arg.Any<AuditLog>(), Arg.Any<CancellationToken>());
        await _auditLogRepo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ── Auditable + Failure → should NOT log ─────────────────────

    [Fact]
    public async Task Handle_AuditableCommandFailure_DoesNotWriteAuditLog()
    {
        var cmd = new TestAuditableCommand();
        RequestHandlerDelegate<Result> next = () => Task.FromResult(Result.Failure("SOME_ERROR"));

        await _behavior.Handle(cmd, next, CancellationToken.None);

        await _auditLogRepo.DidNotReceive().AddAsync(Arg.Any<AuditLog>(), Arg.Any<CancellationToken>());
        await _auditLogRepo.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AuditableGenericResultFailure_DoesNotWriteAuditLog()
    {
        var cmd = new TestAuditableCommandWithGuidResult();
        RequestHandlerDelegate<Result<Guid>> next = () => Task.FromResult(Result<Guid>.Failure("SOME_ERROR"));

        await _behaviorGenericResult.Handle(cmd, next, CancellationToken.None);

        await _auditLogRepo.DidNotReceive().AddAsync(Arg.Any<AuditLog>(), Arg.Any<CancellationToken>());
    }

    // ── Non-auditable → should NOT log ───────────────────────────

    [Fact]
    public async Task Handle_NonAuditableCommand_DoesNotWriteAuditLog()
    {
        var cmd = new TestNonAuditableCommand();
        RequestHandlerDelegate<Result> next = () => Task.FromResult(Result.Success());

        await _behaviorNonAuditable.Handle(cmd, next, CancellationToken.None);

        await _auditLogRepo.DidNotReceive().AddAsync(Arg.Any<AuditLog>(), Arg.Any<CancellationToken>());
    }

    // ── Response passthrough ──────────────────────────────────────

    [Fact]
    public async Task Handle_AlwaysReturnsNextResult()
    {
        var expected = Result.Success();
        var cmd = new TestAuditableCommand();
        RequestHandlerDelegate<Result> next = () => Task.FromResult(expected);

        var actual = await _behavior.Handle(cmd, next, CancellationToken.None);

        Assert.Same(expected, actual);
    }

    // ── Timestamp is set from TimeProvider ───────────────────────

    [Fact]
    public async Task Handle_UsesTimeProviderForTimestamp()
    {
        var expectedTime = new DateTimeOffset(2026, 6, 15, 8, 30, 0, TimeSpan.Zero);
        _time.GetUtcNow().Returns(expectedTime);
        var cmd = new TestAuditableCommand();
        RequestHandlerDelegate<Result> next = () => Task.FromResult(Result.Success());

        await _behavior.Handle(cmd, next, CancellationToken.None);

        await _auditLogRepo.Received(1).AddAsync(
            Arg.Is<AuditLog>(l => l.Timestamp == expectedTime),
            Arg.Any<CancellationToken>());
    }
}

// ── Test Doubles ──────────────────────────────────────────────────

internal sealed record TestAuditableCommand : IRequest<Result>, IAuditableRequest
{
    private static readonly Guid FixedUserId = new("aaaaaaaa-0000-0000-0000-000000000001");
    public Guid? PerformedByUserId => FixedUserId;
    public AuditAction AuditAction => AuditAction.MaintenanceModeChanged;
    public string BuildAuditDetails() => "Test audit details";
}

internal sealed record TestAuditableCommandWithGuidResult : IRequest<Result<Guid>>, IAuditableRequest
{
    private static readonly Guid FixedUserId = new("aaaaaaaa-0000-0000-0000-000000000002");
    public Guid? PerformedByUserId => FixedUserId;
    public AuditAction AuditAction => AuditAction.EmergencyReservationCreated;
    public string BuildAuditDetails() => "Test generic result audit";
}

internal sealed record TestNonAuditableCommand : IRequest<Result>;
