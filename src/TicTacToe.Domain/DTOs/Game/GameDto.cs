using TicTacToe.Domain.Engine;

namespace TicTacToe.Domain.DTOs.Game;

/// <summary>Stateless local/computer payload. JSON: board as int?[] (0=X, 1=O, null=empty).</summary>
public sealed record GameDto(Player?[] Board, Player? Turn, GameResult Result);
