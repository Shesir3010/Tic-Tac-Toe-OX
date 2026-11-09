using TicTacToe.Application.Contracts.Services;
using TicTacToe.Domain.DTOs.Rooms;
using TicTacToe.Domain.Engine;
using TicTacToe.Domain.Online;

namespace TicTacToe.Infrastructure.Services;

/// <summary>In-memory rooms with per-room locks. All state flows through one lock per room.</summary>
public sealed class InMemoryRoomStore : IRoomStore
{
    private readonly Dictionary<Guid, Room> _rooms = new();
    private readonly Dictionary<Guid, object> _locks = new();
    private readonly object _gate = new();

    public (bool ok, RoomState? room, string errorCode) Create(Guid inviterId, Guid targetId)
    {
        lock (_gate)
        {
            if (_rooms.Values.Any(r => r.Status == RoomStatus.Playing && (IsParticipant(r, inviterId) || IsParticipant(r, targetId))))
            {
                return (false, null, "Room.AlreadyActive");
            }

            var room = new Room { PlayerXId = inviterId, PlayerOId = targetId };
            _rooms[room.Id] = room;
            _locks[room.Id] = new object();
            return (true, ToState(room), "");
        }
    }

    public RoomState? Get(Guid id)
    {
        var gate = GetLock(id);
        if (gate is null) return null;
        lock (gate)
        {
            lock (_gate)
            {
                return _rooms.TryGetValue(id, out var r) ? ToState(r) : null;
            }
        }
    }

    public bool IsInGame(Guid userId)
    {
        lock (_gate)
        {
            return _rooms.Values.Any(r => r.Status == RoomStatus.Playing && IsParticipant(r, userId));
        }
    }

    public IReadOnlyList<Guid> GetPlayingRoomIds(Guid userId)
    {
        lock (_gate)
        {
            return _rooms.Values.Where(r => r.Status == RoomStatus.Playing && IsParticipant(r, userId)).Select(r => r.Id).ToList();
        }
    }

    public (RoomState? room, string errorCode, IReadOnlyList<int> moves, GameOutcome outcome, bool finished) ApplyMove(Guid roomId, Guid playerId, int index)
    {
        var gate = GetLock(roomId);
        if (gate is null) return (null, "Room.NotFound", Array.Empty<int>(), GameOutcome.InProgress, false);
        lock (gate)
        {
            var room = Find(roomId);
            if (room is null) return (null, "Room.NotFound", Array.Empty<int>(), GameOutcome.InProgress, false);
            if (!IsParticipant(room, playerId)) return (null, "Room.NotParticipant", Array.Empty<int>(), GameOutcome.InProgress, false);
            if (room.Status != RoomStatus.Playing) return (null, "Room.Expired", Array.Empty<int>(), GameOutcome.InProgress, false);
            var mark = MarkOf(room, playerId);
            var turn = TurnOf(room.Board);
            var validation = Rules.ValidateMove(room.Board, index, mark, turn);
            if (!validation.IsValid) return (null, $"Game.{validation.Failure}", Array.Empty<int>(), GameOutcome.InProgress, false);
            room.Board = Rules.ApplyMove(room.Board, index, mark);
            room.Moves.Add(index);
            var result = Rules.Evaluate(room.Board);
            if (result.IsFinished) room.Status = RoomStatus.Finished;
            return (ToState(room), "", room.Moves.ToArray(), result.Outcome, result.IsFinished);
        }
    }

    public (RoomState? room, string errorCode, bool forfeit, IReadOnlyList<int> moves, GameOutcome outcome) Leave(Guid roomId, Guid playerId)
    {
        var gate = GetLock(roomId);
        if (gate is null) return (null, "Room.NotFound", false, Array.Empty<int>(), GameOutcome.InProgress);
        lock (gate)
        {
            var room = Find(roomId);
            if (room is null) return (null, "Room.NotFound", false, Array.Empty<int>(), GameOutcome.InProgress);
            if (!IsParticipant(room, playerId)) return (null, "Room.NotParticipant", false, Array.Empty<int>(), GameOutcome.InProgress);
            if (room.Status == RoomStatus.Finished)
            {
                room.Status = RoomStatus.Closed;
                return (ToState(room), "", false, room.Moves.ToArray(), Rules.Evaluate(room.Board).Outcome);
            }
            if (room.Status != RoomStatus.Playing) return (null, "Room.Expired", false, Array.Empty<int>(), GameOutcome.InProgress);
            room.Status = RoomStatus.Finished;
            var winnerMark = MarkOf(room, playerId) == Player.X ? Player.O : Player.X;
            var outcome = winnerMark == Player.X ? GameOutcome.XWins : GameOutcome.OWins;
            return (ToState(room), "", true, room.Moves.ToArray(), outcome);
        }
    }

    public (RoomState? room, string errorCode) RequestRematch(Guid roomId, Guid playerId)
    {
        var gate = GetLock(roomId);
        if (gate is null) return (null, "Room.NotFound");
        lock (gate)
        {
            var room = Find(roomId);
            if (room is null) return (null, "Room.NotFound");
            if (!IsParticipant(room, playerId)) return (null, "Room.NotParticipant");
            if (room.Status != RoomStatus.Finished) return (null, "Game.AlreadyOver");
            if (room.RematchInitiatorId is null) room.RematchInitiatorId = playerId;
            if (room.PlayerXId == playerId) room.XRematchRequested = true; else room.ORematchRequested = true;
            if (!(room.XRematchRequested && room.ORematchRequested)) return (ToState(room), "Game.AlreadyOver");
            room.Board = Board.Empty();
            room.Moves.Clear();
            room.Status = RoomStatus.Playing;
            room.XRematchRequested = false;
            room.ORematchRequested = false;
            // The player who requested the rematch begins the next game as X (inviter rule applies to rematch too).
            if (room.RematchInitiatorId is not null && room.RematchInitiatorId != room.PlayerXId)
            {
                (room.PlayerXId, room.PlayerOId) = (room.PlayerOId, room.PlayerXId);
            }
            room.RematchInitiatorId = null;
            return (ToState(room), "");
        }
    }

    public void MarkDisconnected(Guid roomId, Guid playerId, DateTimeOffset deadline)
    {
        var gate = GetLock(roomId);
        if (gate is null) return;
        lock (gate)
        {
            var room = Find(roomId);
            if (room is null || room.Status != RoomStatus.Playing) return;
            if (!IsParticipant(room, playerId)) return;
            if (room.PlayerXId == playerId) room.XDisconnectDeadline = deadline; else room.ODisconnectDeadline = deadline;
        }
    }

    public bool MarkConnected(Guid roomId, Guid playerId)
    {
        var gate = GetLock(roomId);
        if (gate is null) return false;
        lock (gate)
        {
            var room = Find(roomId);
            if (room is null) return false;
            var wasDisconnected = (room.PlayerXId == playerId ? room.XDisconnectDeadline : room.ODisconnectDeadline) is not null;
            if (room.PlayerXId == playerId) room.XDisconnectDeadline = null; else room.ODisconnectDeadline = null;
            return wasDisconnected;
        }
    }

    public DateTimeOffset? GetDisconnectDeadline(Guid roomId, Guid playerId)
    {
        var gate = GetLock(roomId);
        if (gate is null) return null;
        lock (gate)
        {
            var room = Find(roomId);
            if (room is null || room.Status != RoomStatus.Playing) return null;
            return room.PlayerXId == playerId ? room.XDisconnectDeadline : room.PlayerOId == playerId ? room.ODisconnectDeadline : null;
        }
    }

    public RoomSweepResult SweepExpired(TimeProvider clock, int graceSeconds)
    {
        var now = clock.GetUtcNow();
        var forfeits = new List<RoomFinish>();
        var orphans = new List<RoomState>();
        List<Guid> ids;
        lock (_gate) { ids = _rooms.Keys.ToList(); }
        foreach (var id in ids)
        {
            var gate = GetLock(id);
            if (gate is null) continue;
            lock (gate)
            {
                var room = Find(id);
                if (room is null || room.Status != RoomStatus.Playing) continue;
                var xPast = room.XDisconnectDeadline is not null && room.XDisconnectDeadline.Value <= now;
                var oPast = room.ODisconnectDeadline is not null && room.ODisconnectDeadline.Value <= now;
                if (xPast && oPast)
                {
                    room.Status = RoomStatus.Closed;
                    orphans.Add(ToState(room));
                }
                else if (xPast || oPast)
                {
                    var loser = xPast ? room.PlayerXId : room.PlayerOId;
                    var winner = xPast ? room.PlayerOId : room.PlayerXId;
                    room.Status = RoomStatus.Finished;
                    var outcome = winner == room.PlayerXId ? GameOutcome.XWins : GameOutcome.OWins;
                    forfeits.Add(new RoomFinish(ToState(room), room.Moves.ToArray(), winner, loser));
                    room.XDisconnectDeadline = null;
                    room.ODisconnectDeadline = null;
                    _ = outcome;
                }
            }
        }
        return new RoomSweepResult(forfeits, orphans);
    }

    private object? GetLock(Guid id)
    {
        lock (_gate) { return _locks.TryGetValue(id, out var gate) ? gate : null; }
    }

    private Room? Find(Guid id)
    {
        lock (_gate) { return _rooms.TryGetValue(id, out var r) ? r : null; }
    }

    private static bool IsParticipant(Room r, Guid playerId) => r.PlayerXId == playerId || r.PlayerOId == playerId;
    private static Player MarkOf(Room r, Guid playerId) => r.PlayerXId == playerId ? Player.X : Player.O;

    private static Player TurnOf(Board board)
    {
        var x = board.Cells.Count(c => c == Player.X);
        var o = board.Cells.Count(c => c == Player.O);
        return x == o ? Player.X : Player.O;
    }

    // Copy under caller-held room lock where needed; simple read path clones via ToArray.
    private static RoomState ToState(Room r) => new(
        r.Id, r.Status, r.Board.Cells.ToArray(),
        r.Status == RoomStatus.Playing ? TurnOf(r.Board) : null,
        r.PlayerXId, r.PlayerOId,
        (r.Status == RoomStatus.Finished || r.Status == RoomStatus.Closed) ? Rules.Evaluate(r.Board) : null);
}
