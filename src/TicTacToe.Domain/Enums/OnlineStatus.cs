namespace TicTacToe.Domain.Enums;

/// <summary>Lobby statuses (FR-32): derived from presence + invitations + rooms. Invitations/rooms land M10/M11; Presence starts Available-only.</summary>
public enum OnlineStatus
{
    Available,
    Invited,
    InGame,
}
