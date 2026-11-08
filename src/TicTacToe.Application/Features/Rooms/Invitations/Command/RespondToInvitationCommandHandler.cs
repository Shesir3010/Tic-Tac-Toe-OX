using Cortex.Mediator.Commands;
using TicTacToe.Application.Common.Results;
using TicTacToe.Application.Contracts.Services;
using TicTacToe.Domain.DTOs.Rooms;

namespace TicTacToe.Application.Features.Rooms.Invitations.Command;

public sealed class RespondToInvitationCommandHandler : ICommandHandler<RespondToInvitationCommand, OperationResult<RoomState>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IInvitationManager _invitations;
    private readonly IRoomStore _rooms;
    private readonly TimeProvider _clock;

    public RespondToInvitationCommandHandler(ICurrentUser currentUser, IInvitationManager invitations, IRoomStore rooms, TimeProvider clock)
    {
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _invitations = invitations ?? throw new ArgumentNullException(nameof(invitations));
        _rooms = rooms ?? throw new ArgumentNullException(nameof(rooms));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public Task<OperationResult<RoomState>> Handle(RespondToInvitationCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (_currentUser.UserId is null)
        {
            return Task.FromResult(OperationResult<RoomState>.Failure(GameErrors.Validation("Login is required.")));
        }

        // Lock order (ADR-0014): invitation lock first (inside Respond), room lock second (Create).
        var (ok, inv, code) = _invitations.Respond(command.InvitationId, _currentUser.UserId.Value, command.Accept, _clock);
        if (!ok)
        {
            return Task.FromResult(OperationResult<RoomState>.Failure(new OperationError(code, code switch
            {
                "Room.Expired" => "Room not found.",
                "Invite.Forbidden" => "You can't do that.",
                "Invite.Offline" => "Player is offline.",
                _ => "Room not found.",
            })));
        }

        if (!command.Accept)
        {
            return Task.FromResult(OperationResult<RoomState>.Success(null!));
        }

        var (created, room, roomError) = _rooms.Create(inv!.FromUserId, inv.ToUserId);
        if (!created)
        {
            return Task.FromResult(OperationResult<RoomState>.Failure(new OperationError(roomError, "You already have a game in progress.")));
        }

        return Task.FromResult(OperationResult<RoomState>.Success(room!));
    }
}
