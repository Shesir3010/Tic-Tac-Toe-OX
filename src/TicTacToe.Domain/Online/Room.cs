using TicTacToe.Domain.Engine;

namespace TicTacToe.Domain.Online;

/// <summary>Pure room model (in-memory only). Inviter starts as X; rematch initiator begins the next game as X.</summary>
public sealed class Room
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PlayerXId { get; set; }
    public Guid PlayerOId { get; set; }
    public Board Board { get; set; } = Board.Empty();
    public RoomStatus Status { get; set; } = RoomStatus.Playing;
    public List<int> Moves { get; set; } = new();
    public bool XRematchRequested { get; set; }
    public bool ORematchRequested { get; set; }
    public Guid? RematchInitiatorId { get; set; }
    public DateTimeOffset? XDisconnectDeadline { get; set; }
    public DateTimeOffset? ODisconnectDeadline { get; set; }
}
