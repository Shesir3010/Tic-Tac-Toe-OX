using TicTacToe.Domain.DTOs.Rooms;

namespace TicTacToe.Application.Contracts.Services;

/// <summary>Online presence: userId → connectionCount (≥1 = online). In-memory singleton, single instance.</summary>
public interface IPresenceTracker
{
    void AddUser(Guid userId, string displayName);
    void RemoveUser(Guid userId);
    int ConnectionCount(Guid userId);
    IReadOnlyList<OnlinePlayerDto> GetOnline();
}
