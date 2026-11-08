using Cortex.Mediator.Commands;
using TicTacToe.Application.Common.Results;

namespace TicTacToe.Application.Features.Rooms.Online.Command;

/// <summary>Leave: mid-game = forfeit (opponent wins, both rows once); finished = close only.</summary>
public sealed record LeaveRoomCommand(Guid RoomId) : ICommand<OperationResult>;
