namespace TicTacToe.Domain.Online;

public enum InvitationStatus
{
    Pending,
    Accepted,
    Rejected,
    Expired,
    Cancelled,
}

/// <summary>Pure invitation state (no locks/I/O in Domain; manager owns lifecycle).</summary>
public sealed class Invitation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid FromUserId { get; set; }
    public Guid ToUserId { get; set; }
    public InvitationStatus Status { get; set; } = InvitationStatus.Pending;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset ExpiresAtUtc { get; set; }
}
