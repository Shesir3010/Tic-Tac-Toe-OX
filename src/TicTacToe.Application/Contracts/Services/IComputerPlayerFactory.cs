using TicTacToe.Domain.Contracts;
using TicTacToe.Domain.Engine;
using TicTacToe.Domain.Engine.Ai;
using TicTacToe.Domain.Enums;

namespace TicTacToe.Application.Contracts.Services;

/// <summary>Resolves difficulty → player. Owns the OCP entry point; Easy gets the shared IRandomSource.</summary>
public interface IComputerPlayerFactory
{
    IComputerPlayer Create(Difficulty difficulty, Player mark);
}
