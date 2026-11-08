namespace TicTacToe.Domain.Enums;

/// <summary>Stored mode (0 Local, 1 Computer, 2 Online). GameMode covers PlayMove; Online rows are written by the room finish.</summary>
public enum MatchMode
{
    Local,
    Computer,
    Online,
}
