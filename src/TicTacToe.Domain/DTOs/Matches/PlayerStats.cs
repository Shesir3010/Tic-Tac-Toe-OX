namespace TicTacToe.Domain.DTOs.Matches;

/// <summary>Totals computed from Matches rows, never stored (FR-16).</summary>
public sealed record PlayerStats(int Played, int Wins, int Losses, int Draws);
