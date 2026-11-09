using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TicTacToe.Application.Contracts.Services;
using TicTacToe.Web.Models;

namespace TicTacToe.Web.Controllers;

public class HomeController(IRoomStore rooms) : Controller
{
    public IActionResult Index()
    {
        if (Guid.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var userId))
        {
            var active = rooms.GetPlayingRoomIds(userId).FirstOrDefault();
            ViewBag.ActiveRoomId = active == Guid.Empty ? null : active.ToString();
        }
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
