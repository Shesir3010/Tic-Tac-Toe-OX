using TicTacToe.Domain.Online;

namespace TicTacToe.Application.Contracts.Services;

/// <summary>Invitation guards, TTL, cooldown, atomic accept (lock order: invitation first, room second).</summary>
public interface IInvitationManager
{
    (bool ok, Invitation? invitation, string errorCode) Send(Guid fromUserId, Guid toUserId, TimeProvider clock);
    (bool ok, string errorCode) Cancel(Guid invitationId, Guid inviterUserId);
    (bool ok, Invitation? invitation, string errorCode) Respond(Guid invitationId, Guid targetUserId, bool accept, TimeProvider clock);
    void ExpirePending(TimeProvider clock);
    void CancelForOffline(Guid userId);
    Invitation? Get(Guid invitationId);
    IReadOnlyList<Invitation> GetPendingForUser(Guid userId);
}
