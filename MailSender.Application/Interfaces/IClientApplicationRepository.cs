using MailSender.Domain.Entities;

namespace MailSender.Application.Interfaces;

public interface IClientApplicationRepository
{
    Task AddAsync(ClientApplication application);

    Task<ClientApplication?> GetByAppIdAsync(string appId);
}