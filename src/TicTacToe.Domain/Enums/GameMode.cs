namespace TicTacToe.Domain.Enums;

/// <summary>Game channel (0 Local, 1 Computer). Online moves never use PlayMove; they go via MakeOnlineMove).</summary>
public enum GameMode
{
    Local,
    Computer,
}
