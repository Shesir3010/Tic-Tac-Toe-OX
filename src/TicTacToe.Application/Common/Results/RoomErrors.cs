namespace TicTacToe.Application.Common.Results;

/// <summary>Room.* catalog (error-handling.md §2). Literal copy here is the single source for hub/handlers/tests.</summary>
public static class RoomErrors
{
    public static OperationError NotFound => new("Room.NotFound", "Room not found.");
    public static OperationError Expired => new("Room.Expired", "This room is no longer active.");
    public static OperationError NotParticipant => new("Room.NotParticipant", "You are not playing in this room.");
    public static OperationError AlreadyActive => new("Room.AlreadyActive", "You already have a game in progress.");
}
