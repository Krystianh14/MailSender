using MailSender.Application.Interfaces;
using MailSender.Domain.Entities;
using MailSender.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MailSender.Infrastructure.Repositories;

public class EfMailLogRepository : IMailLogRepository
{
    private readonly MailSenderDbContext _dbContext;

    public EfMailLogRepository(MailSenderDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(MailSendLog log)
    {
        await _dbContext.MailSendLogs.AddAsync(log);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<List<MailSendLog>> GetByClientApplicationIdAsync(Guid clientApplicationId)
    {
        return await _dbContext.MailSendLogs
            .Where(x => x.ClientApplicationId == clientApplicationId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync();
    }

    public async Task<MailSendLog?> GetByIdAndClientApplicationIdAsync(Guid logId, Guid clientApplicationId)
    {
        return await _dbContext.MailSendLogs
            .FirstOrDefaultAsync(x =>
                x.Id == logId &&
                x.ClientApplicationId == clientApplicationId
            );
    }
}