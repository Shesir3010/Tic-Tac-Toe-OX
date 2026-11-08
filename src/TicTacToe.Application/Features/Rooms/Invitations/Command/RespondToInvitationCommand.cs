using Cortex.Mediator.Commands;
using TicTacToe.Application.Common.Results;
using TicTacToe.Domain.DTOs.Rooms;

namespace TicTacToe.Application.Features.Rooms.Invitations.Command;

/// <summary>Accept → OperationResult&lt;RoomState&gt;; Reject → OperationResult (plain).</summary>
public sealed record RespondToInvitationCommand(Guid InvitationId, bool Accept) : ICommand<OperationResult<RoomState>>;
