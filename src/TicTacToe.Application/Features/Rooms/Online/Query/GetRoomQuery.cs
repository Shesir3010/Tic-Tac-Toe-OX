using Cortex.Mediator.Commands;
using TicTacToe.Application.Common.Results;
using TicTacToe.Domain.DTOs.Rooms;

namespace TicTacToe.Application.Features.Rooms.Online.Query;

/// <summary>Resync source of truth — clients rebuild state only from this, never from memory.</summary>
public sealed record GetRoomQuery(Guid RoomId) : ICommand<OperationResult<RoomState>>;
