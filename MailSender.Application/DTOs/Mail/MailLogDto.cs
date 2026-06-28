namespace MailSender.Application.DTOs.MailLogs;

public record MailLogDto(
    Guid Id,
    string AppId,
    string AppName,
    string To,
    string Subject,
    string Body,
    string Status,
    string? ErrorMessage,
    DateTime CreatedAtUtc
);