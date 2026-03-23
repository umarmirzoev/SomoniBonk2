namespace SomoniBank.Application.AI.Interfaces;

public interface ICurrentUserService
{
    string? UserId { get; }
    string? PhoneNumber { get; }
    string? Role { get; }
}
