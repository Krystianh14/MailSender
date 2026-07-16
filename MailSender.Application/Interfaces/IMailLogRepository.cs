using MailSender.Domain.Entities;

namespace MailSender.Application.Interfaces;

public interface IMailLogRepository
{
    Task AddAsync(MailSendLog log);

    Task<List<MailSendLog>> GetByClientApplicationIdAsync(Guid clientApplicationId);

    Task<MailSendLog?> GetByIdAndClientApplicationIdAsync(Guid logId, Guid clientApplicationId);
}