using Microsoft.Extensions.Logging;
using SomoniBank.Infrastructure.Interfaces;

namespace SomoniBank.Infrastructure.Services;

public class MockSmsSender(ILogger<MockSmsSender> logger) : ISmsSender
{
    public Task SendAsync(string phone, string message)
    {
        logger.LogInformation("SMS to {Phone}: {Message}", phone, message);
        return Task.CompletedTask;
    }
}
