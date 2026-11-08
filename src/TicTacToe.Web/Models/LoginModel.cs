using System.ComponentModel.DataAnnotations;

namespace TicTacToe.Web.Models;

public sealed class LoginModel
{
    [Required]
    public string UsernameOrEmail { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Remember me")]
    public bool RememberMe { get; set; }
}
