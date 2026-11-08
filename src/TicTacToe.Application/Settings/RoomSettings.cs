namespace TicTacToe.Application.Settings;

/// <summary>All M9-M11 timings (config, never literals).</summary>
public sealed class RoomSettings
{
    public int GraceSeconds { get; set; } = 60;
    public int InvitationTtlSeconds { get; set; } = 30;
    public int CooldownSeconds { get; set; } = 30;
    public int CleanupIntervalSeconds { get; set; } = 5;
}
