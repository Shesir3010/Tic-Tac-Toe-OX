namespace TicTacToe.Application.Common.Results;

public sealed class OperationResult<T> : OperationResult
{
    private OperationResult(bool isSuccess, T? value, OperationError? error)
        : base(isSuccess, error)
    {
        Value = value;
    }

    public T? Value { get; }

    public static OperationResult<T> Success(T value) => new(true, value, null);

    public static new OperationResult<T> Failure(OperationError error) => new(false, default, error);

    public static new OperationResult<T> Failure(string code, string message) => new(false, default, new OperationError(code, message));
}
