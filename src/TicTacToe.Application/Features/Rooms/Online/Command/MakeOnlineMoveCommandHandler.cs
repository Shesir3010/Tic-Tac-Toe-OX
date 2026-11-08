using Cortex.Mediator.Commands;
using TicTacToe.Application.Common.Results;
using TicTacToe.Application.Contracts;
using TicTacToe.Application.Contracts.Repositories;
using TicTacToe.Application.Contracts.Services;
using TicTacToe.Domain.DTOs.Rooms;
using TicTacToe.Domain.Entities;
using TicTacToe.Domain.Engine;
using TicTacToe.Domain.Enums;

namespace TicTacToe.Application.Features.Rooms.Online.Command;

/// <summary>Applies the move under the room lock; on finish saves both Match rows once (one SaveChanges).</summary>
public sealed class MakeOnlineMoveCommandHandler : ICommandHandler<MakeOnlineMoveCommand, OperationResult<RoomState>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IRoomStore _rooms;
    private readonly IMatchRepository _matches;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _clock;

    public MakeOnlineMoveCommandHandler(ICurrentUser currentUser, IRoomStore rooms, IMatchRepository matches, IUnitOfWork unitOfWork, TimeProvider clock)
    {
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _rooms = rooms ?? throw new ArgumentNullException(nameof(rooms));
        _matches = matches ?? throw new ArgumentNullException(nameof(matches));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public async Task<OperationResult<RoomState>> Handle(MakeOnlineMoveCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (_currentUser.UserId is null)
        {
            return OperationResult<RoomState>.Failure(GameErrors.Validation("Login is required."));
        }

        var (room, code, moves, outcome, finished) = _rooms.ApplyMove(command.RoomId, _currentUser.UserId.Value, command.Index);
        if (room is null)
        {
            return OperationResult<RoomState>.Failure(Map(code));
        }

        if (finished)
        {
            await SaveBothRowsAsync(room, moves, outcome, cancellationToken);
        }
        return OperationResult<RoomState>.Success(room);
    }

    private async Task SaveBothRowsAsync(RoomState room, IReadOnlyList<int> moves, GameOutcome outcome, CancellationToken ct)
    {
        var now = _clock.GetUtcNow();
        var csv = string.Join(",", moves);
        await _matches.AddAsync(new Match
        {
            UserId = room.PlayerXId,
            OpponentUserId = room.PlayerOId,
            Mode = MatchMode.Online,
            Symbol = Player.X,
            Result = outcome == GameOutcome.XWins ? MatchResult.Win : outcome == GameOutcome.Draw ? MatchResult.Draw : MatchResult.Loss,
            Moves = csv,
            PlayedAtUtc = now,
        }, ct);
        await _matches.AddAsync(new Match
        {
            UserId = room.PlayerOId,
            OpponentUserId = room.PlayerXId,
            Mode = MatchMode.Online,
            Symbol = Player.O,
            Result = outcome == GameOutcome.OWins ? MatchResult.Win : outcome == GameOutcome.Draw ? MatchResult.Draw : MatchResult.Loss,
            Moves = csv,
            PlayedAtUtc = now,
        }, ct);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    private static OperationError Map(string code) => code switch
    {
        "Room.NotFound" => RoomErrors.NotFound,
        "Room.Expired" => RoomErrors.Expired,
        "Room.NotParticipant" => RoomErrors.NotParticipant,
        "Game.NotYourTurn" => GameErrors.NotYourTurn,
        "Game.CellOccupied" => GameErrors.CellOccupied,
        "Game.InvalidCell" => GameErrors.InvalidCell,
        "Game.AlreadyOver" => GameErrors.AlreadyOver,
        _ => new OperationError(code, code),
    };
}
