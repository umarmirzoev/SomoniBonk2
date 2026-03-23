using System.ComponentModel.DataAnnotations;

namespace SomoniBank.Domain.DTOs;

public class UserInsertDto
{
    [Required]
    [StringLength(50, MinimumLength = 2)]
    public string FirstName { get; set; } = null!;

    [Required]
    [StringLength(50, MinimumLength = 2)]
    public string LastName { get; set; } = null!;

    [Required]
    [EmailAddress]
    [StringLength(100)]
    public string Email { get; set; } = null!;

    [Required]
    [StringLength(100, MinimumLength = 6)]
    public string Password { get; set; } = null!;

    [Required]
    [StringLength(20, MinimumLength = 5)]
    public string Phone { get; set; } = null!;

    [Required]
    [StringLength(200, MinimumLength = 3)]
    public string Address { get; set; } = null!;

    [Required]
    [StringLength(20, MinimumLength = 5)]
    public string PassportNumber { get; set; } = null!;
}

public class UserUpdateDto
{
    public Guid Id { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
}

public class UserGetDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public string Address { get; set; } = null!;
    public string PassportNumber { get; set; } = null!;
    public string Role { get; set; } = null!;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}

public class AuthResponseDto
{
    public string Token { get; set; } = null!;
    public string Role { get; set; } = null!;
    public Guid UserId { get; set; }
    public string FullName { get; set; } = null!;
}
