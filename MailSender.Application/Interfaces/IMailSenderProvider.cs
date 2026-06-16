using MailSender.Domain.Entities;

namespace MailSender.Application.Interfaces;

public interface IMailSenderProvider
{
    Task SendAsync(MailMessage message);
}