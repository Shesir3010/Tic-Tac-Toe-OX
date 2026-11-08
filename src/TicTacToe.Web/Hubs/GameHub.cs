namespace TicTacToe.Web.Hubs;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

[Authorize]
public sealed class GameHub : Hub
{
}
