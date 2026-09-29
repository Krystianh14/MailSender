using System.Collections.Concurrent;
using MailSender.Application.Interfaces;
using MailSender.Domain.Entities;

namespace MailSender.IntegrationTests.Infrastructure;

public sealed class TestMailSenderProvider : IMailSenderProvider
{
    public ConcurrentQueue<MailMessage> Messages { get; } = new();
    public string? FailureMessage { get; set; }

    public Task SendAsync(MailMessage message)
    {
        Messages.Enqueue(message);
        return FailureMessage is null
            ? Task.CompletedTask
            : Task.FromException(new InvalidOperationException(FailureMessage));
    }
}
