using TicTacToe.Application.Contracts.Services;
using TicTacToe.Domain.DTOs.Rooms;
using TicTacToe.Domain.Engine;
using TicTacToe.Domain.Online;

namespace TicTacToe.Infrastructure.Services;

public sealed class InMemoryRoomStore : IRoomStore
{
    private readonly Dictionary<Guid, Room> _rooms = new();
    private readonly object _gate = new();

    public (bool ok, RoomState? room, string errorCode) Create(Guid inviterId, Guid targetId)
    {
        lock (_gate)
        {
            if (_rooms.Values.Any(r => r.Status == RoomStatus.Playing && (r.PlayerXId == inviterId || r.PlayerOId == inviterId || r.PlayerXId == targetId || r.PlayerOId == targetId)))
            {
                return (false, null, "Room.AlreadyActive");
            }

            var room = new Room { PlayerXId = inviterId, PlayerOId = targetId };
            _rooms[room.Id] = room;
            return (true, ToState(room), "");
        }
    }

    public RoomState? Get(Guid id)
    {
        lock (_gate)
        {
            return _rooms.TryGetValue(id, out var r) ? ToState(r) : null;
        }
    }

    private static RoomState ToState(Room r) => new(
        r.Id, r.Status, r.Board.Cells.ToArray(),
        r.Status == RoomStatus.Playing ? Player.X : null,
        r.PlayerXId, r.PlayerOId,
        r.Status == RoomStatus.Finished ? Rules.Evaluate(r.Board) : null);
}
