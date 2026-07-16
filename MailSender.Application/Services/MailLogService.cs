using MailSender.Application.Common;
using MailSender.Application.DTOs.MailLogs;
using MailSender.Application.Interfaces;
using MailSender.Domain.Entities;

namespace MailSender.Application.Services;

public class MailLogService
{
    private readonly IMailLogRepository _mailLogRepository;

    public MailLogService(IMailLogRepository mailLogRepository)
    {
        _mailLogRepository = mailLogRepository;
    }

    public async Task<ServiceResult<List<MailLogDto>>> GetLogsAsync(ClientApplication application)
    {
        var logs = await _mailLogRepository.GetByClientApplicationIdAsync(application.Id);

        var result = logs
            .Select(MapToDto)
            .ToList();

        return ServiceResult<List<MailLogDto>>.Success(result);
    }

    public async Task<ServiceResult<MailLogDto>> GetLogByIdAsync(
        Guid id,
        ClientApplication application)
    {
        var log = await _mailLogRepository.GetByIdAndClientApplicationIdAsync(
            id,
            application.Id
        );

        if (log is null)
        {
            return ServiceResult<MailLogDto>.Failure(
                "Mail log not found.");
        }

        return ServiceResult<MailLogDto>.Success(MapToDto(log));
    }

    private static MailLogDto MapToDto(MailSendLog log)
    {
        return new MailLogDto(
            log.Id,
            log.AppId,
            log.AppName,
            log.To,
            log.Subject,
            log.Body,
            log.Status,
            log.ErrorMessage,
            log.CreatedAtUtc
        );
    }
}