namespace MailSender.Application.DTOs.Mail;

public record SendMailResponse(
    string AppId,
    string AppName,
    string Status,
    MailDto Email
);