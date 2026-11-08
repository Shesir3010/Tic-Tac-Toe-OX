using TicTacToe.Domain.Enums;

namespace TicTacToe.Domain.DTOs.Rooms;

/// <summary>Lobby row (username only, never email). Status derived server-side, never trusted from client.</summary>
public sealed record OnlinePlayerDto(Guid UserId, string DisplayName, OnlineStatus Status);
