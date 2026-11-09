using TicTacToe.Domain.DTOs.Rooms;
using TicTacToe.Domain.Engine;
using TicTacToe.Domain.Online;

namespace TicTacToe.Application.Contracts.Services;

/// <summary>Room store with per-room locks (lock → validate → mutate → commit → release → broadcast, §5).</summary>
public interface IRoomStore
{
    (bool ok, RoomState? room, string errorCode) Create(Guid inviterId, Guid targetId);
    RoomState? Get(Guid id);
    bool IsInGame(Guid userId);
    IReadOnlyList<Guid> GetPlayingRoomIds(Guid userId);
    (RoomState? room, string errorCode, IReadOnlyList<int> moves, GameOutcome outcome, bool finished) ApplyMove(Guid roomId, Guid playerId, int index);
    (RoomState? room, string errorCode, bool forfeit, IReadOnlyList<int> moves, GameOutcome outcome) Leave(Guid roomId, Guid playerId);
    (RoomState? room, string errorCode) RequestRematch(Guid roomId, Guid playerId);
    void MarkDisconnected(Guid roomId, Guid playerId, DateTimeOffset deadline);
    bool MarkConnected(Guid roomId, Guid playerId);
    RoomSweepResult SweepExpired(TimeProvider clock, int graceSeconds);
}

public sealed record RoomSweepResult(
    IReadOnlyList<RoomFinish> ForfeitFinishes,
    IReadOnlyList<RoomState> OrphanCloses);

public sealed record RoomFinish(
    RoomState Room,
    IReadOnlyList<int> Moves,
    Guid WinnerId,
    Guid LoserId);
