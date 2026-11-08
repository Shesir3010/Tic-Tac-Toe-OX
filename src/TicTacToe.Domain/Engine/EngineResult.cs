namespace TicTacToe.Domain.Engine;

/// <summary>Expected-failure vocabulary shared by transport (maps to Game.* codes in ADR-0010).</summary>
public enum EngineFailure
{
    InvalidCell,
    CellOccupied,
    NotYourTurn,
    AlreadyOver,
}

/// <summary>Return value of <see cref="Rules.ValidateMove"/>; never throws.</summary>
public sealed record EngineResult(bool IsValid, EngineFailure? Failure)
{
    public static EngineResult Success() => new(true, null);

    public static EngineResult Fail(EngineFailure failure) => new(false, failure);
}
