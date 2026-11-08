using TicTacToe.Application.Contracts.Services;
using TicTacToe.Domain.Contracts;
using TicTacToe.Domain.Engine;
using TicTacToe.Domain.Engine.Ai;
using TicTacToe.Domain.Enums;

namespace TicTacToe.Infrastructure.Services;

public sealed class ComputerPlayerFactory : IComputerPlayerFactory
{
    private readonly IRandomSource _random;

    public ComputerPlayerFactory(IRandomSource random) => _random = random ?? throw new ArgumentNullException(nameof(random));

    public IComputerPlayer Create(Difficulty difficulty, Player mark) => difficulty switch
    {
        Difficulty.Easy => new EasyAi(mark, _random),
        Difficulty.Medium => new MediumAi(mark),
        Difficulty.Hard => new HardAi(mark),
        _ => throw new ArgumentOutOfRangeException(nameof(difficulty)),
    };
}
