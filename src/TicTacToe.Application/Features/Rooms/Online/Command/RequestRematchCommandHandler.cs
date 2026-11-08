using Cortex.Mediator.Commands;
using TicTacToe.Application.Common.Results;
using TicTacToe.Application.Contracts.Services;
using TicTacToe.Domain.DTOs.Rooms;

namespace TicTacToe.Application.Features.Rooms.Online.Command;

public sealed class RequestRematchCommandHandler : ICommandHandler<RequestRematchCommand, OperationResult<RoomState>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IRoomStore _rooms;

    public RequestRematchCommandHandler(ICurrentUser currentUser, IRoomStore rooms)
    {
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _rooms = rooms ?? throw new ArgumentNullException(nameof(rooms));
    }

    public Task<OperationResult<RoomState>> Handle(RequestRematchCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (_currentUser.UserId is null)
        {
            return Task.FromResult(OperationResult<RoomState>.Failure(GameErrors.Validation("Login is required.")));
        }

        var (room, code) = _rooms.RequestRematch(command.RoomId, _currentUser.UserId.Value);
        if (room is null)
        {
            return Task.FromResult(OperationResult<RoomState>.Failure(code switch
            {
                "Room.NotFound" => RoomErrors.NotFound,
                "Room.NotParticipant" => RoomErrors.NotParticipant,
                "Game.AlreadyOver" => GameErrors.AlreadyOver,
                _ => new OperationError(code, code),
            }));
        }
        return Task.FromResult(OperationResult<RoomState>.Success(room));
    }
}
