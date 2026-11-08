using Cortex.Mediator.Commands;
using TicTacToe.Application.Common.Results;
using TicTacToe.Application.Contracts.Services;
using TicTacToe.Domain.DTOs.Rooms;
using TicTacToe.Domain.Enums;

namespace TicTacToe.Application.Features.Rooms.Invitations.Query;

public sealed class GetOnlinePlayersQueryHandler : ICommandHandler<GetOnlinePlayersQuery, OperationResult<IReadOnlyList<OnlinePlayerDto>>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IPresenceTracker _presence;
    private readonly IInvitationManager _invitations;
    private readonly IRoomStore _rooms;

    public GetOnlinePlayersQueryHandler(ICurrentUser currentUser, IPresenceTracker presence, IInvitationManager invitations, IRoomStore rooms)
    {
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _presence = presence ?? throw new ArgumentNullException(nameof(presence));
        _invitations = invitations ?? throw new ArgumentNullException(nameof(invitations));
        _rooms = rooms ?? throw new ArgumentNullException(nameof(rooms));
    }

    public Task<OperationResult<IReadOnlyList<OnlinePlayerDto>>> Handle(GetOnlinePlayersQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (_currentUser.UserId is null)
        {
            return Task.FromResult(OperationResult<IReadOnlyList<OnlinePlayerDto>>.Failure(GameErrors.Validation("Login is required to view online players.")));
        }

        var rows = _presence.GetOnline().Select(p =>
        {
            var status = _rooms.IsInGame(p.UserId)
                ? OnlineStatus.InGame
                : _invitations.GetPendingForUser(p.UserId).Count > 0
                    ? OnlineStatus.Invited
                    : OnlineStatus.Available;
            return new OnlinePlayerDto(p.UserId, p.DisplayName, status);
        }).ToList();
        return Task.FromResult(OperationResult<IReadOnlyList<OnlinePlayerDto>>.Success(rows));
    }
}
