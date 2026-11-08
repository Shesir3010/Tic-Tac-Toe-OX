using Cortex.Mediator.Commands;
using TicTacToe.Application.Common.Results;
using TicTacToe.Application.Contracts.Repositories;
using TicTacToe.Application.Contracts.Services;
using TicTacToe.Domain.DTOs.Matches;

namespace TicTacToe.Application.Features.Matches.History.Query;

public sealed class GetStatsQueryHandler : ICommandHandler<GetStatsQuery, OperationResult<PlayerStats>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IMatchRepository _matches;

    public GetStatsQueryHandler(ICurrentUser currentUser, IMatchRepository matches)
    {
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _matches = matches ?? throw new ArgumentNullException(nameof(matches));
    }

    public async Task<OperationResult<PlayerStats>> Handle(GetStatsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (_currentUser.UserId is null)
        {
            return OperationResult<PlayerStats>.Failure(GameErrors.Validation("Login is required to view stats."));
        }

        var stats = await _matches.GetStatsAsync(_currentUser.UserId.Value, cancellationToken);
        return OperationResult<PlayerStats>.Success(stats);
    }
}
