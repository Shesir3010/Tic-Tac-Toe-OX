using TicTacToe.Domain.Engine;
using TicTacToe.Domain.Enums;

namespace TicTacToe.Web.Models;

/// <summary>JSON body for POST /api/game/move. Cells: 0=X, 1=O, null=empty. Mode/Difficulty default to Local.</summary>
public sealed class MoveRequest
{
    public Player?[] Cells { get; set; } = [];

    public int MoveIndex { get; set; }

    public Player Mark { get; set; }

    public GameMode Mode { get; set; }

    public Difficulty? Difficulty { get; set; }
}
