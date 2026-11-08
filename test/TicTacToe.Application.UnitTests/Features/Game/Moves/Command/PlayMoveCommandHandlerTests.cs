using NUnit.Framework;
using Shouldly;
using TicTacToe.Application.Common.Results;
using TicTacToe.Application.Features.Game.Moves.Command;
using TicTacToe.Domain.Engine;

namespace TicTacToe.Application.UnitTests.Features.Game.Moves.Command;

[TestFixture]
public sealed class PlayMoveCommandHandlerTests
{
    [Test]
    public async Task Handle_ValidMove_ReturnsSuccessWithNewBoard()
    {
        var result = await new PlayMoveCommandHandler().Handle(
            new PlayMoveCommand(new Player?[9], 4, Player.X), default);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Board[4].ShouldBe(Player.X);
        result.Value.Turn.ShouldBe(Player.O);
        result.Value.Result.Outcome.ShouldBe(GameOutcome.InProgress);
    }

    [Test]
    public async Task Handle_InvalidIndex_ReturnsFailure()
    {
        var result = await new PlayMoveCommandHandler().Handle(
            new PlayMoveCommand(new Player?[9], 9, Player.X), default);

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("Game.InvalidCell");
    }

    [Test]
    public async Task Handle_Occupied_ReturnsFailure()
    {
        var cells = new Player?[9];
        cells[0] = Player.X;

        var result = await new PlayMoveCommandHandler().Handle(
            new PlayMoveCommand(cells, 0, Player.O), default);

        result.Error!.Code.ShouldBe("Game.CellOccupied");
    }

    [Test]
    public async Task Handle_WrongMark_ReturnsFailure()
    {
        var result = await new PlayMoveCommandHandler().Handle(
            new PlayMoveCommand(new Player?[9], 0, Player.O), default);

        result.Error!.Code.ShouldBe("Game.NotYourTurn");
    }

    [Test]
    public async Task Handle_AfterWin_ReturnsFailure()
    {
        var board = Board.Empty()
            .Move(0, Player.X).Move(3, Player.O).Move(1, Player.X)
            .Move(4, Player.O).Move(2, Player.X);

        var result = await new PlayMoveCommandHandler().Handle(
            new PlayMoveCommand(board.Cells.ToArray(), 5, Player.O), default);

        result.Error!.Code.ShouldBe("Game.AlreadyOver");
    }

    [Test]
    public async Task Handle_WrongLengthBoard_ReturnsValidation()
    {
        var result = await new PlayMoveCommandHandler().Handle(
            new PlayMoveCommand(new Player?[8], 0, Player.X), default);

        result.Error!.Code.ShouldBe("Validation.Error");
    }

    [Test]
    public async Task Handle_WinningMove_ReturnsWinAndNullTurn()
    {
        var board = new Board(
        [
            Player.X, Player.X, null,
            Player.O, Player.O, null,
            null, null, null,
        ]);

        var result = await new PlayMoveCommandHandler().Handle(
            new PlayMoveCommand(board.Cells.ToArray(), 2, Player.X), default);

        result.Value!.Result.Outcome.ShouldBe(GameOutcome.XWins);
        result.Value.Result.WinningLine!.ShouldBe(new[] { 0, 1, 2 });
        result.Value.Turn.ShouldBeNull();
    }

    [Test]
    public async Task Handle_NullCommand_Throws()
    {
        Should.Throw<ArgumentNullException>(() => new PlayMoveCommandHandler().Handle(null!, default));
    }
}
