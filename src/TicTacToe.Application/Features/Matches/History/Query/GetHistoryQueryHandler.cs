using Cortex.Mediator.Commands;
using TicTacToe.Application.Common.Results;
using TicTacToe.Application.Contracts.Repositories;
using TicTacToe.Application.Contracts.Services;
using TicTacToe.Domain.DTOs.Matches;

namespace TicTacToe.Application.Features.Matches.History.Query;

public sealed class GetHistoryQueryHandler : ICommandHandler<GetHistoryQuery, OperationResult<IReadOnlyList<MatchHistoryItem>>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IMatchRepository _matches;

    public GetHistoryQueryHandler(ICurrentUser currentUser, IMatchRepository matches)
    {
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _matches = matches ?? throw new ArgumentNullException(nameof(matches));
    }

    public async Task<OperationResult<IReadOnlyList<MatchHistoryItem>>> Handle(GetHistoryQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (_currentUser.UserId is null)
        {
            return OperationResult<IReadOnlyList<MatchHistoryItem>>.Failure(GameErrors.Validation("Login is required to view history."));
        }

        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 50);
        var items = await _matches.GetHistoryAsync(_currentUser.UserId.Value, page, pageSize, cancellationToken);
        return OperationResult<IReadOnlyList<MatchHistoryItem>>.Success(items);
    }
}
