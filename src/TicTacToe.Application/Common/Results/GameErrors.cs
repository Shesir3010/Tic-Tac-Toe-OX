namespace TicTacToe.Application.Common.Results;

/// <summary>Game.* catalog (error-handling.md §2). Literal copy here is the single source for controllers/hub/tests.</summary>
public static class GameErrors
{
    public static OperationError NotYourTurn => new("Game.NotYourTurn", "It's not your turn.");
    public static OperationError CellOccupied => new("Game.CellOccupied", "That cell is already taken.");
    public static OperationError AlreadyOver => new("Game.AlreadyOver", "This game is already finished.");
    public static OperationError InvalidCell => new("Game.InvalidCell", "That cell doesn't exist.");
    public static OperationError ForgedResult => new("Game.ForgedResult", "The submitted result doesn't match the moves.");
    public static OperationError Validation(string message) => new("Validation.Error", message);
}
