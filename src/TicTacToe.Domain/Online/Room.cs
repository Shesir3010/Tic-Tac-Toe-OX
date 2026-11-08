using TicTacToe.Domain.Engine;

namespace TicTacToe.Domain.Online;

/// <summary>Pure room model (in-memory only). Inviter is always X.</summary>
public sealed class Room
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PlayerXId { get; set; }
    public Guid PlayerOId { get; set; }
    public Board Board { get; set; } = Board.Empty();
    public RoomStatus Status { get; set; } = RoomStatus.Playing;
}
