namespace TicTacToe.Domain.DTOs.Rooms;

/// <summary>Invitation wire shape (camelCase by JSON).</summary>
public sealed record InvitationStateDto(Guid InvitationId, Guid FromUserId, Guid ToUserId, string Status, int ExpiresInSeconds);
