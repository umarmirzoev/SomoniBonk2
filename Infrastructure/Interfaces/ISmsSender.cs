namespace SomoniBank.Infrastructure.Interfaces;

public interface ISmsSender
{
    Task SendAsync(string phone, string message);
}
