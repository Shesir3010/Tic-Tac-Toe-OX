namespace TicTacToe.Domain.Engine;

/// <summary>Immutable 3x3 board (cells 0-8, row-major). Value semantics; <see cref="Move"/> returns a new board.</summary>
public sealed class Board : IEquatable<Board>
{
    private readonly Player?[] _cells;

    public Board(IEnumerable<Player?> cells)
    {
        ArgumentNullException.ThrowIfNull(cells);
        var materialized = cells.ToArray();
        if (materialized.Length != 9)
        {
            throw new ArgumentException("A board has exactly 9 cells.", nameof(cells));
        }

        _cells = materialized;
    }

    public static Board Empty() => new(new Player?[9]);

    public Player? this[int index] => _cells[index];

    public IReadOnlyList<Player?> Cells => _cells;

    public Board Move(int index, Player player)
    {
        if (index < 0 || index >= _cells.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index), "Cell index must be between 0 and 8.");
        }

        if (_cells[index] is not null)
        {
            throw new InvalidOperationException("That cell is already taken.");
        }

        var next = (Player?[])_cells.Clone();
        next[index] = player;
        return new Board(next);
    }

    public bool Equals(Board? other)
    {
        if (other is null)
        {
            return false;
        }

        return _cells.SequenceEqual(other._cells);
    }

    public override bool Equals(object? obj) => Equals(obj as Board);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var cell in _cells)
        {
            hash.Add(cell);
        }

        return hash.ToHashCode();
    }

    public static bool operator ==(Board? left, Board? right) => left?.Equals(right) ?? right is null;

    public static bool operator !=(Board? left, Board? right) => !(left == right);
}
