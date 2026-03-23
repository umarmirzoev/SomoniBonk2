using System.ComponentModel.DataAnnotations;

namespace SomoniBank.Application.AI.DTOs;

public class AiAskRequestDto
{
    [Required]
    [StringLength(1000, MinimumLength = 3)]
    public string UserQuestion { get; set; } = string.Empty;

    [StringLength(10)]
    public string? Language { get; set; }
}
