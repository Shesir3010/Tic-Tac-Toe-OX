using TicTacToe.Domain.Engine;
using TicTacToe.Domain.Online;

namespace TicTacToe.Domain.DTOs.Rooms;

/// <summary>Wire shape for online play (single version for caller + broadcast, per realtime-signalr §2.1).</summary>
public sealed record RoomState(
    Guid Id,
    RoomStatus Status,
    Player?[] Board,
    Player? Turn,
    Guid PlayerXId,
    Guid PlayerOId,
    GameResult? Result);
