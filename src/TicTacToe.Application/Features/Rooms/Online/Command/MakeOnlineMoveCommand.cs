using Cortex.Mediator.Commands;
using TicTacToe.Application.Common.Results;
using TicTacToe.Domain.DTOs.Rooms;

namespace TicTacToe.Application.Features.Rooms.Online.Command;

/// <summary>Online move: roomId + cell index; the room's per-room lock validates and mutates.</summary>
public sealed record MakeOnlineMoveCommand(Guid RoomId, int Index) : ICommand<OperationResult<RoomState>>;
