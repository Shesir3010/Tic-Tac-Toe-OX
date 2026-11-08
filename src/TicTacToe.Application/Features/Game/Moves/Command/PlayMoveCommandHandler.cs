using Cortex.Mediator.Commands;
using TicTacToe.Application.Common.Results;
using TicTacToe.Application.Contracts.Services;
using TicTacToe.Domain.DTOs.Game;
using TicTacToe.Domain.Engine;
using TicTacToe.Domain.Enums;

namespace TicTacToe.Application.Features.Game.Moves.Command;

public sealed class PlayMoveCommandHandler : ICommandHandler<PlayMoveCommand, OperationResult<GameDto>>
{
    private readonly IComputerPlayerFactory _factory;

    public PlayMoveCommandHandler(IComputerPlayerFactory factory) => _factory = factory ?? throw new ArgumentNullException(nameof(factory));

    public async Task<OperationResult<GameDto>> Handle(PlayMoveCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.Cells is null || command.Cells.Length != 9)
        {
            return OperationResult<GameDto>.Failure(
                GameErrors.Validation("Board must have 9 cells."));
        }

        var board = new Board(command.Cells);
        var turn = TurnOf(board);

        // E2 guard: a forged board whose X/O counts break strict alternation is not your turn.
        var xCount = board.Cells.Count(c => c == Player.X);
        var oCount = board.Cells.Count(c => c == Player.O);
        if (xCount < oCount || xCount > oCount + 1)
        {
            return OperationResult<GameDto>.Failure(GameErrors.NotYourTurn);
        }

        var validation = Rules.ValidateMove(board, command.MoveIndex, command.Mark, turn);
        if (!validation.IsValid)
        {
            return OperationResult<GameDto>.Failure(ToError(validation.Failure));
        }

        var next = Rules.ApplyMove(board, command.MoveIndex, command.Mark);
        var result = Rules.Evaluate(next);
        int? computerMoveIndex = null;

        if (command.Mode == GameMode.Computer && !result.IsFinished)
        {
            // FR-10: deliberate 300-500ms thinking delay (board is non-interactive meanwhile via client busy).
            // 300ms keeps total under the 500ms budget with minimax (~150ms worst case).
            await Task.Delay(300, cancellationToken);

            var difficulty = command.Difficulty ?? Difficulty.Medium;
            var computerMark = command.Mark == Player.X ? Player.O : Player.X;
            var computer = _factory.Create(difficulty, computerMark);
            var computerMove = computer.ChooseMove(next);
            next = Rules.ApplyMove(next, computerMove, computerMark);
            computerMoveIndex = computerMove;
            result = Rules.Evaluate(next);
        }

        var nextTurn = result.IsFinished
            ? (Player?)null
            : command.Mode == GameMode.Computer
                ? command.Mark
                : (command.Mark == Player.X ? Player.O : Player.X);

        var dto = new GameDto(next.Cells.ToArray(), nextTurn, result, computerMoveIndex);
        return OperationResult<GameDto>.Success(dto);
    }

    private static OperationError ToError(EngineFailure? failure) => failure switch
    {
        EngineFailure.InvalidCell => GameErrors.InvalidCell,
        EngineFailure.CellOccupied => GameErrors.CellOccupied,
        EngineFailure.AlreadyOver => GameErrors.AlreadyOver,
        EngineFailure.NotYourTurn => GameErrors.NotYourTurn,
        _ => GameErrors.Validation("Illegal move."),
    };

    private static Player TurnOf(Board board)
    {
        var xCount = 0;
        var oCount = 0;
        foreach (var cell in board.Cells)
        {
            if (cell == Player.X)
            {
                xCount++;
            }
            else if (cell == Player.O)
            {
                oCount++;
            }
        }

        return xCount == oCount ? Player.X : Player.O;
    }
}
