using TicTacToe.Domain.DTOs.Rooms;
using TicTacToe.Domain.Online;

namespace TicTacToe.Application.Contracts.Services;

/// <summary>In-memory rooms (M10: create-only for atomic accept; moves/leave/rematch land M11).</summary>
public interface IRoomStore
{
    (bool ok, RoomState? room, string errorCode) Create(Guid inviterId, Guid targetId);
    RoomState? Get(Guid id);
}
