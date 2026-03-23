using System.ComponentModel.DataAnnotations;

namespace SomoniBank.Domain.DTOs;

public class ChangePasswordDto
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string OldPassword { get; set; } = null!;

    [Required]
    [StringLength(100, MinimumLength = 6)]
    public string NewPassword { get; set; } = null!;

    [Required]
    [StringLength(100, MinimumLength = 6)]
    public string ConfirmPassword { get; set; } = null!;
}
