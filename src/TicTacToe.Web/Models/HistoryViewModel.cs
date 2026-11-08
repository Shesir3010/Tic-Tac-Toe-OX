using TicTacToe.Domain.DTOs.Matches;

namespace TicTacToe.Web.Models;

public sealed record HistoryViewModel(
    IReadOnlyList<MatchHistoryItem> Items,
    string Error,
    PlayerStats? Stats,
    int Page);
