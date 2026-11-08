using TicTacToe.Domain.Engine;

namespace TicTacToe.Web.Models;

/// <summary>JSON body for POST /api/game/move. Cells: 0=X, 1=O, null=empty.</summary>
public sealed class MoveRequest
{
    public Player?[] Cells { get; set; } = [];

    public int MoveIndex { get; set; }

    public Player Mark { get; set; }
}
