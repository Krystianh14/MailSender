using MailSender.Application.Interfaces;
using MailSender.Domain.Entities;
using MailSender.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MailSender.Infrastructure.Repositories;

public class EfClientApplicationRepository : IClientApplicationRepository
{
    private readonly MailSenderDbContext _dbContext;

    public EfClientApplicationRepository(MailSenderDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(ClientApplication application)
    {
        await _dbContext.ClientApplications.AddAsync(application);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<ClientApplication?> GetByIdAsync(Guid id)
    {
        return await _dbContext.ClientApplications
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<ClientApplication?> GetByAppIdAsync(string appId)
    {
        return await _dbContext.ClientApplications
            .FirstOrDefaultAsync(x => x.AppId == appId);
    }

    public async Task<ClientApplication?> GetByAppNameAsync(string appName)
    {
        return await _dbContext.ClientApplications
            .FirstOrDefaultAsync(x => x.AppName == appName);
    }
}