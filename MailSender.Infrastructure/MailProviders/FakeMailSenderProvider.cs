using MailSender.Application.Interfaces;
using MailSender.Domain.Entities;

namespace MailSender.Infrastructure.MailProviders;

public class FakeMailSenderProvider : IMailSenderProvider
{
    public Task SendAsync(MailMessage message)
    {
        Console.WriteLine("Fake email sent:");
        Console.WriteLine($"To: {message.To}");
        Console.WriteLine($"Subject: {message.Subject}");
        Console.WriteLine($"Body: {message.Body}");

        return Task.CompletedTask;
    }
}