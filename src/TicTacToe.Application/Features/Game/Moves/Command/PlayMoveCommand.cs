using Cortex.Mediator.Commands;
using TicTacToe.Application.Common.Results;
using TicTacToe.Domain.DTOs.Game;
using TicTacToe.Domain.Engine;

namespace TicTacToe.Application.Features.Game.Moves.Command;

/// <summary>Stateless local move: browser sends the current board plus the move; server is referee.</summary>
public sealed record PlayMoveCommand(Player?[] Cells, int MoveIndex, Player Mark) : ICommand<OperationResult<GameDto>>;
