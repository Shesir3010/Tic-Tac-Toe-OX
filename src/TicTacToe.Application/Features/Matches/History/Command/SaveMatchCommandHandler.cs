using Cortex.Mediator.Commands;
using TicTacToe.Application.Common.Results;
using TicTacToe.Application.Contracts;
using TicTacToe.Application.Contracts.Repositories;
using TicTacToe.Application.Contracts.Services;
using TicTacToe.Domain.Entities;
using TicTacToe.Domain.Engine;
using TicTacToe.Domain.Enums;

namespace TicTacToe.Application.Features.Matches.History.Command;

/// <summary>Forgery proof: replays Moves through the engine; any illegality/unfinished/mismatch = ForgedResult, no row written.</summary>
public sealed class SaveMatchCommandHandler : ICommandHandler<SaveMatchCommand, OperationResult>
{
    private readonly ICurrentUser _currentUser;
    private readonly IMatchRepository _matches;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _clock;

    public SaveMatchCommandHandler(ICurrentUser currentUser, IMatchRepository matches, IUnitOfWork unitOfWork, TimeProvider clock)
    {
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _matches = matches ?? throw new ArgumentNullException(nameof(matches));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public async Task<OperationResult> Handle(SaveMatchCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (_currentUser.UserId is null)
        {
            return OperationResult.Failure(GameErrors.Validation("Login is required to save a match."));
        }

        if (command.Moves is null || command.Moves.Length is < 5 or > 9)
        {
            return OperationResult.Failure(GameErrors.ForgedResult);
        }

        var board = Board.Empty();
        for (var i = 0; i < command.Moves.Length; i++)
        {
            var mark = i % 2 == 0 ? Player.X : Player.O;
            var turn = i % 2 == 0 ? Player.X : Player.O;
            if (!Rules.ValidateMove(board, command.Moves[i], mark, turn).IsValid)
            {
                return OperationResult.Failure(GameErrors.ForgedResult);
            }

            board = Rules.ApplyMove(board, command.Moves[i], mark);
        }

        var outcome = Rules.Evaluate(board);
        if (!outcome.IsFinished)
        {
            return OperationResult.Failure(GameErrors.ForgedResult);
        }

        var trueResult = TrueResult(outcome.Outcome, command.Symbol);
        if (trueResult != command.ClaimedResult)
        {
            return OperationResult.Failure(GameErrors.ForgedResult);
        }

        var match = new Match
        {
            UserId = _currentUser.UserId.Value,
            OpponentUserId = command.OpponentUserId,
            Mode = (MatchMode)command.Mode,
            Difficulty = command.Mode == GameMode.Computer ? command.Difficulty : null,
            Symbol = command.Symbol,
            Result = trueResult,
            Moves = string.Join(",", command.Moves),
            PlayedAtUtc = _clock.GetUtcNow(),
        };

        await _matches.AddAsync(match, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult.Success();
    }

    private static MatchResult TrueResult(GameOutcome outcome, Player symbol) => outcome switch
    {
        GameOutcome.XWins => symbol == Player.X ? MatchResult.Win : MatchResult.Loss,
        GameOutcome.OWins => symbol == Player.O ? MatchResult.Win : MatchResult.Loss,
        _ => MatchResult.Draw,
    };
}
