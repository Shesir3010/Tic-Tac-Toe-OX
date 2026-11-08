using Microsoft.Extensions.Options;
using TicTacToe.Application.Contracts.Services;
using TicTacToe.Application.Settings;
using TicTacToe.Domain.Online;

namespace TicTacToe.Infrastructure.Services;

public sealed class InvitationManager(IPresenceTracker presence, IOptions<RoomSettings> settings) : IInvitationManager
{
    private readonly Dictionary<Guid, Invitation> _pending = new();
    private readonly Dictionary<(Guid From, Guid To), DateTimeOffset> _cooldowns = new();
    private readonly object _gate = new();

    public (bool ok, Invitation? invitation, string errorCode) Send(Guid from, Guid to, TimeProvider clock)
    {
        lock (_gate)
        {
            ExpirePending(clock);

            if (from == to) return (false, null, "Invite.Self");
            if (presence.ConnectionCount(to) == 0) return (false, null, "Invite.Offline");
            if (presence.ConnectionCount(from) == 0) return (false, null, "Invite.Offline");
            if (_pending.Values.Any(i => i.FromUserId == from)) return (false, null, "Invite.AlreadyPending");
            if (_pending.Values.Any(i => i.ToUserId == to || i.FromUserId == to)) return (false, null, "Invite.Busy");
            if (_cooldowns.TryGetValue((from, to), out var until) && until > clock.GetUtcNow()) return (false, null, "Invite.Cooldown");

            var inv = new Invitation
            {
                FromUserId = from,
                ToUserId = to,
                CreatedAtUtc = clock.GetUtcNow(),
            };
            inv.ExpiresAtUtc = inv.CreatedAtUtc.AddSeconds(settings.Value.InvitationTtlSeconds);
            _pending[inv.Id] = inv;
            return (true, inv, "");
        }
    }

    public (bool ok, string errorCode) Cancel(Guid id, Guid inviter)
    {
        lock (_gate)
        {
            if (!_pending.TryGetValue(id, out var inv)) return (false, "Invite.Forbidden");
            if (inv.FromUserId != inviter) return (false, "Invite.Forbidden");
            inv.Status = InvitationStatus.Cancelled;
            _pending.Remove(id);
            return (true, "");
        }
    }

    public (bool ok, Invitation? invitation, string errorCode) Respond(Guid id, Guid target, bool accept, TimeProvider clock)
    {
        lock (_gate)
        {
            if (!_pending.TryGetValue(id, out var inv)) return (false, null, "Room.Expired");
            if (inv.ToUserId != target) return (false, null, "Invite.Forbidden");
            if (presence.ConnectionCount(target) == 0)
            {
                inv.Status = InvitationStatus.Cancelled;
                _pending.Remove(id);
                return (false, null, "Invite.Offline");
            }

            inv.Status = accept ? InvitationStatus.Accepted : InvitationStatus.Rejected;
            _pending.Remove(id);
            if (!accept)
            {
                _cooldowns[(inv.FromUserId, inv.ToUserId)] = clock.GetUtcNow().AddSeconds(settings.Value.CooldownSeconds);
            }

            return (true, inv, "");
        }
    }

    public void ExpirePending(TimeProvider clock)
    {
        lock (_gate)
        {
            foreach (var (id, inv) in _pending.ToList())
            {
                if (inv.ExpiresAtUtc <= clock.GetUtcNow())
                {
                    inv.Status = InvitationStatus.Expired;
                    _pending.Remove(id);
                }
            }
        }
    }

    public void CancelForOffline(Guid userId)
    {
        lock (_gate)
        {
            foreach (var (id, inv) in _pending.ToList())
            {
                if (inv.FromUserId == userId || inv.ToUserId == userId)
                {
                    inv.Status = InvitationStatus.Cancelled;
                    _pending.Remove(id);
                }
            }
        }
    }
}
