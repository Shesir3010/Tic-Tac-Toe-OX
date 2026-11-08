using Cortex.Mediator.Commands;
using TicTacToe.Application.Common.Results;
using TicTacToe.Domain.DTOs.Rooms;

namespace TicTacToe.Application.Features.Rooms.Invitations.Command;

/// <summary>Hub InvitePlayer: userId → InvitationState (waiting).</summary>
public sealed record SendInvitationCommand(Guid TargetUserId) : ICommand<OperationResult<InvitationStateDto>>;
