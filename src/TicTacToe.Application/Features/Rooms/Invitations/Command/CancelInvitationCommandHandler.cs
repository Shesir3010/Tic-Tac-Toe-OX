using Cortex.Mediator.Commands;
using TicTacToe.Application.Common.Results;
using TicTacToe.Application.Contracts.Services;

namespace TicTacToe.Application.Features.Rooms.Invitations.Command;

public sealed class CancelInvitationCommandHandler : ICommandHandler<CancelInvitationCommand, OperationResult>
{
    private readonly ICurrentUser _currentUser;
    private readonly IInvitationManager _invitations;

    public CancelInvitationCommandHandler(ICurrentUser currentUser, IInvitationManager invitations)
    {
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _invitations = invitations ?? throw new ArgumentNullException(nameof(invitations));
    }

    public Task<OperationResult> Handle(CancelInvitationCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (_currentUser.UserId is null)
        {
            return Task.FromResult(OperationResult.Failure(GameErrors.Validation("Login is required.")));
        }

        var (ok, code) = _invitations.Cancel(command.InvitationId, _currentUser.UserId.Value);
        return Task.FromResult(ok ? OperationResult.Success() : OperationResult.Failure(code, "You can't do that."));
    }
}
