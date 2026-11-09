using System.ComponentModel.DataAnnotations;

namespace TicTacToe.Web.Models;

public sealed class DeleteAccountModel
{
    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string ConfirmUsername { get; set; } = string.Empty;
}
