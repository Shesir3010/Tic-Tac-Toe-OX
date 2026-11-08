using System.ComponentModel.DataAnnotations;

namespace TicTacToe.Web.Models;

public sealed class RegisterModel
{
    [Required]
    [StringLength(20, MinimumLength = 3)]
    [RegularExpression("^[A-Za-z0-9_]+$", ErrorMessage = "Letters, digits and underscore only.")]
    public string Username { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 8)]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
