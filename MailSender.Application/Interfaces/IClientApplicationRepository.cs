using MailSender.Domain.Entities;

namespace MailSender.Application.Interfaces;

public interface IClientApplicationRepository
{
    Task AddAsync(ClientApplication application);

    Task<ClientApplication?> GetByIdAsync(Guid id);

    Task<ClientApplication?> GetByAppIdAsync(string appId);

    Task<ClientApplication?> GetByAppNameAsync(string appName);
}