using Cortex.Mediator.Commands;
using TicTacToe.Application.Common.Results;
using TicTacToe.Domain.DTOs.Matches;

namespace TicTacToe.Application.Features.Matches.History.Query;

public sealed record GetStatsQuery : ICommand<OperationResult<PlayerStats>>;
