using Cortex.Mediator.Commands;
using TicTacToe.Application.Common.Results;
using TicTacToe.Domain.Engine;
using TicTacToe.Domain.Enums;

namespace TicTacToe.Application.Features.Matches.History.Command;

/// <summary>Razor POST: client submits the finished game's move list + claimed result; server replays and verifies.</summary>
public sealed record SaveMatchCommand(
    int[] Moves,
    MatchResult ClaimedResult,
    Player Symbol,
    GameMode Mode,
    Difficulty? Difficulty,
    Guid? OpponentUserId) : ICommand<OperationResult>;
