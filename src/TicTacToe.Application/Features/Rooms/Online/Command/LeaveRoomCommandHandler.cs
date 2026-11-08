using Cortex.Mediator.Commands;
using TicTacToe.Application.Common.Results;
using TicTacToe.Application.Contracts;
using TicTacToe.Application.Contracts.Repositories;
using TicTacToe.Application.Contracts.Services;
using TicTacToe.Domain.Entities;
using TicTacToe.Domain.Engine;
using TicTacToe.Domain.Enums;

namespace TicTacToe.Application.Features.Rooms.Online.Command;

public sealed class LeaveRoomCommandHandler : ICommandHandler<LeaveRoomCommand, OperationResult>
{
    private readonly ICurrentUser _currentUser;
    private readonly IRoomStore _rooms;
    private readonly IMatchRepository _matches;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _clock;

    public LeaveRoomCommandHandler(ICurrentUser currentUser, IRoomStore rooms, IMatchRepository matches, IUnitOfWork unitOfWork, TimeProvider clock)
    {
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _rooms = rooms ?? throw new ArgumentNullException(nameof(rooms));
        _matches = matches ?? throw new ArgumentNullException(nameof(matches));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public async Task<OperationResult> Handle(LeaveRoomCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (_currentUser.UserId is null)
        {
            return OperationResult.Failure(GameErrors.Validation("Login is required."));
        }

        var (room, code, forfeit, moves, outcome) = _rooms.Leave(command.RoomId, _currentUser.UserId.Value);
        if (room is null)
        {
            return OperationResult.Failure(code switch
            {
                "Room.NotFound" => RoomErrors.NotFound,
                "Room.Expired" => RoomErrors.Expired,
                "Room.NotParticipant" => RoomErrors.NotParticipant,
                _ => new OperationError(code, code),
            });
        }

        if (forfeit)
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
            }, cancellationToken);
            await _matches.AddAsync(new Match
            {
                UserId = room.PlayerOId,
                OpponentUserId = room.PlayerXId,
                Mode = MatchMode.Online,
                Symbol = Player.O,
                Result = outcome == GameOutcome.OWins ? MatchResult.Win : outcome == GameOutcome.Draw ? MatchResult.Draw : MatchResult.Loss,
                Moves = csv,
                PlayedAtUtc = now,
            }, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        return OperationResult.Success();
    }
}
