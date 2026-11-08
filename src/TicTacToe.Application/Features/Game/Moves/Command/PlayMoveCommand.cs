using Cortex.Mediator.Commands;
using TicTacToe.Application.Common.Results;
using TicTacToe.Domain.DTOs.Game;
using TicTacToe.Domain.Engine;
using TicTacToe.Domain.Enums;

namespace TicTacToe.Application.Features.Game.Moves.Command;

/// <summary>Stateless move (local or vs computer): browser sends the current board plus the move; server is referee.</summary>
public sealed record PlayMoveCommand(
    Player?[] Cells,
    int MoveIndex,
    Player Mark,
    GameMode Mode = GameMode.Local,
    Difficulty? Difficulty = null) : ICommand<OperationResult<GameDto>>;
