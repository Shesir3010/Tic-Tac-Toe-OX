using Cortex.Mediator.Commands;
using TicTacToe.Application.Common.Results;
using TicTacToe.Application.Contracts.Services;
using TicTacToe.Domain.DTOs.Rooms;

namespace TicTacToe.Application.Features.Rooms.Invitations.Query;

public sealed class GetOnlinePlayersQueryHandler : ICommandHandler<GetOnlinePlayersQuery, OperationResult<IReadOnlyList<OnlinePlayerDto>>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IPresenceTracker _presence;

    public GetOnlinePlayersQueryHandler(ICurrentUser currentUser, IPresenceTracker presence)
    {
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _presence = presence ?? throw new ArgumentNullException(nameof(presence));
    }

    public Task<OperationResult<IReadOnlyList<OnlinePlayerDto>>> Handle(GetOnlinePlayersQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (_currentUser.UserId is null)
        {
            return Task.FromResult(OperationResult<IReadOnlyList<OnlinePlayerDto>>.Failure(GameErrors.Validation("Login is required to view online players.")));
        }

        return Task.FromResult(OperationResult<IReadOnlyList<OnlinePlayerDto>>.Success(_presence.GetOnline()));
    }
}
