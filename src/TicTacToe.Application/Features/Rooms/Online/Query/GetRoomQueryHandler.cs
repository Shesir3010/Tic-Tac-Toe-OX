using Cortex.Mediator.Commands;
using TicTacToe.Application.Common.Results;
using TicTacToe.Application.Contracts.Services;
using TicTacToe.Domain.DTOs.Rooms;

namespace TicTacToe.Application.Features.Rooms.Online.Query;

public sealed class GetRoomQueryHandler : ICommandHandler<GetRoomQuery, OperationResult<RoomState>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IRoomStore _rooms;

    public GetRoomQueryHandler(ICurrentUser currentUser, IRoomStore rooms)
    {
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _rooms = rooms ?? throw new ArgumentNullException(nameof(rooms));
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
        if (room.PlayerXId != _currentUser.UserId.Value && room.PlayerOId != _currentUser.UserId.Value)
        {
            return Task.FromResult(OperationResult<RoomState>.Failure(RoomErrors.NotParticipant));
        }
        return Task.FromResult(OperationResult<RoomState>.Success(room));
    }
}
