using Cortex.Mediator.Commands;
using TicTacToe.Application.Common.Results;
using TicTacToe.Domain.DTOs.Rooms;

namespace TicTacToe.Application.Features.Rooms.Online.Command;

/// <summary>Rematch: both-ready resets the same room (X to start).</summary>
public sealed record RequestRematchCommand(Guid RoomId) : ICommand<OperationResult<RoomState>>;
