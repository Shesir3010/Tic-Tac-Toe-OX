using TicTacToe.Application.Contracts.Services;
using TicTacToe.Domain.DTOs.Rooms;
using TicTacToe.Domain.Enums;

namespace TicTacToe.Infrastructure.Services;

public sealed class PresenceTracker : IPresenceTracker
{
    private readonly Dictionary<Guid, Presence> _users = new();
    private readonly object _gate = new();

    public int ConnectionCount(Guid userId)
    {
        lock (_gate)
        {
            return _users.TryGetValue(userId, out var p) ? p.Count : 0;
        }
    }

    public IReadOnlyList<OnlinePlayerDto> GetOnline()
    {
        lock (_gate)
        {
            return _users
                .Where(kv => kv.Value.Count > 0)
                .Select(kv => new OnlinePlayerDto(kv.Key, kv.Value.DisplayName, OnlineStatus.Available))
                .OrderBy(p => p.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }

    public void AddUser(Guid userId, string displayName)
    {
        lock (_gate)
        {
            if (_users.TryGetValue(userId, out var p))
            {
                p.Count++;
                if (!string.IsNullOrEmpty(displayName)) p.DisplayName = displayName;
            }
            else
            {
                _users[userId] = new Presence { DisplayName = displayName, Count = 1 };
            }
        }
    }

    public void RemoveUser(Guid userId)
    {
        lock (_gate)
        {
            if (!_users.TryGetValue(userId, out var p)) return;
            p.Count--;
            if (p.Count <= 0) _users.Remove(userId);
        }
    }

    private sealed class Presence
    {
        public string DisplayName { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}
