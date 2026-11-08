using Cortex.Mediator.Commands;
using TicTacToe.Application.Common.Results;
using TicTacToe.Domain.DTOs.Matches;

namespace TicTacToe.Application.Features.Matches.History.Query;

public sealed record GetHistoryQuery(int Page = 1, int PageSize = 10) : ICommand<OperationResult<IReadOnlyList<MatchHistoryItem>>>;
