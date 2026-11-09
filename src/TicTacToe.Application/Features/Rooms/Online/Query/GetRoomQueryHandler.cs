using Cortex.Mediator.Commands;
using TicTacToe.Application.Common.Results;
using TicTacToe.Application.Contracts.Services;
using TicTacToe.Domain.DTOs.Rooms;

namespace TicTacToe.Application.Features.Rooms.Online.Query;

public sealed class GetRoomQueryHandler : ICommandHandler<GetRoomQuery, OperationResult<RoomState>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IRoomStore _rooms;
    private readonly TimeProvider _clock;

    public GetRoomQueryHandler(ICurrentUser currentUser, IRoomStore rooms, TimeProvider clock)
    {
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _rooms = rooms ?? throw new ArgumentNullException(nameof(rooms));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public Task<OperationResult<RoomState>> Handle(GetRoomQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (_currentUser.UserId is null)
        {
            return Task.FromResult(OperationResult<RoomState>.Failure(GameErrors.Validation("Login is required.")));
        }

        var room = _rooms.Get(query.RoomId);
        if (room is null) return Task.FromResult(OperationResult<RoomState>.Failure(RoomErrors.NotFound));
        var me = _currentUser.UserId.Value;
        if (room.PlayerXId != me && room.PlayerOId != me)
        {
            return Task.FromResult(OperationResult<RoomState>.Failure(RoomErrors.NotParticipant));
        }
        var opponent = room.PlayerXId == me ? room.PlayerOId : room.PlayerXId;
        var deadline = _rooms.GetDisconnectDeadline(query.RoomId, opponent);
        var secondsLeft = deadline is null ? (int?)null : Math.Max(0, (int)Math.Ceiling((deadline.Value - _clock.GetUtcNow()).TotalSeconds));
        var withGrace = room with { OppDisconnectSecondsLeft = secondsLeft };
        return Task.FromResult(OperationResult<RoomState>.Success(withGrace));
    }
}
