using TicTacToe.Domain.Engine;

namespace TicTacToe.Domain.Engine.Ai;

/// <summary>One computer difficulty. OCP: new difficulty = new class + one factory entry.</summary>
public interface IComputerPlayer
{
    Player Mark { get; }

    /// <returns>A legal cell index (0-8) for the board's side to move.</returns>
    int ChooseMove(Board board);
}
