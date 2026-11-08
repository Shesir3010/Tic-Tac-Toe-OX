using TicTacToe.Domain.Engine;

namespace TicTacToe.Domain.DTOs.Game;

/// <summary>Stateless local/computer payload. Board: 0=X, 1=O, null=empty. ComputerMoveIndex set in Computer mode.</summary>
public sealed record GameDto(Player?[] Board, Player? Turn, GameResult Result, int? ComputerMoveIndex = null);
