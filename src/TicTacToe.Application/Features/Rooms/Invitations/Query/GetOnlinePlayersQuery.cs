using Cortex.Mediator.Commands;
using TicTacToe.Application.Common.Results;
using TicTacToe.Application.Contracts.Services;
using TicTacToe.Domain.DTOs.Rooms;

namespace TicTacToe.Application.Features.Rooms.Invitations.Query;

/// <summary>Registered only; status derived server-side from PresenceTracker (+ InvitationManager + RoomStore at M10/M11).</summary>
public sealed record GetOnlinePlayersQuery : ICommand<OperationResult<IReadOnlyList<OnlinePlayerDto>>>;
