using MailSender.Application.Interfaces;
using MailSender.Domain.Entities;

namespace MailSender.Infrastructure.Repositories;

public class InMemoryClientApplicationRepository : IClientApplicationRepository
{
    private readonly Dictionary<string, ClientApplication> _applications = new();

    public Task AddAsync(ClientApplication application)
    {
        _applications[application.AppId] = application;
        return Task.CompletedTask;
    }

    public Task<ClientApplication?> GetByAppIdAsync(string appId)
    {
        _applications.TryGetValue(appId, out var application);
        return Task.FromResult(application);
    }

    public Task<ClientApplication?> GetByIdAsync(Guid id)
    {
        var application = _applications.Values
            .FirstOrDefault(a => a.Id == id);
        return Task.FromResult(application);
    }

    public Task<ClientApplication?> GetByAppNameAsync(string appName)
    {
        var application = _applications.Values
            .FirstOrDefault(a => a.AppName == appName);
        return Task.FromResult(application);
    }
}