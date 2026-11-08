namespace TicTacToe.Application.Common.Results;

/// <summary>One literal error (code + user message). Copy lives here and in 02-backend/error-handling.md.</summary>
public sealed record OperationError(string Code, string Message);

public class OperationResult
{
    protected OperationResult(bool isSuccess, OperationError? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }

    public OperationError? Error { get; }

    public static OperationResult Success() => new(true, null);

    public static OperationResult Failure(OperationError error) => new(false, error);

    public static OperationResult Failure(string code, string message) => new(false, new OperationError(code, message));
}
