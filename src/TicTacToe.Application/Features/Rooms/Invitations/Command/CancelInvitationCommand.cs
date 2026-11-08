using Cortex.Mediator.Commands;
using TicTacToe.Application.Common.Results;

namespace TicTacToe.Application.Features.Rooms.Invitations.Command;

public sealed record CancelInvitationCommand(Guid InvitationId) : ICommand<OperationResult>;
