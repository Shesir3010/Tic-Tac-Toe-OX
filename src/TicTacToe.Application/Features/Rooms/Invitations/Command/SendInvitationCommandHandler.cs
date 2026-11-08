using Cortex.Mediator.Commands;
using TicTacToe.Application.Common.Results;
using TicTacToe.Application.Contracts.Services;
using TicTacToe.Domain.DTOs.Rooms;

namespace TicTacToe.Application.Features.Rooms.Invitations.Command;

public sealed class SendInvitationCommandHandler : ICommandHandler<SendInvitationCommand, OperationResult<InvitationStateDto>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IInvitationManager _invitations;
    private readonly TimeProvider _clock;

    public SendInvitationCommandHandler(ICurrentUser currentUser, IInvitationManager invitations, TimeProvider clock)
    {
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _invitations = invitations ?? throw new ArgumentNullException(nameof(invitations));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public Task<OperationResult<InvitationStateDto>> Handle(SendInvitationCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (_currentUser.UserId is null)
        {
            return Task.FromResult(OperationResult<InvitationStateDto>.Failure(GameErrors.Validation("Login is required to invite players.")));
        }

        var (ok, inv, code) = _invitations.Send(_currentUser.UserId.Value, command.TargetUserId, _clock);
        if (!ok)
        {
            return Task.FromResult(OperationResult<InvitationStateDto>.Failure(new OperationError(code, InviteMessage(code))));
        }

        return Task.FromResult(OperationResult<InvitationStateDto>.Success(
            new InvitationStateDto(inv!.Id, inv.FromUserId, inv.ToUserId, inv.Status.ToString(), 30)));
    }

    private static string InviteMessage(string code) => code switch
    {
        "Invite.Self" => "You can't invite yourself.",
        "Invite.Offline" => "Player is offline.",
        "Invite.Busy" => "Player is busy right now.",
        "Invite.Cooldown" => "You can try inviting again in a moment.",
        "Invite.AlreadyPending" => "You already have an invitation waiting.",
        _ => "You can't do that.",
    };
}
