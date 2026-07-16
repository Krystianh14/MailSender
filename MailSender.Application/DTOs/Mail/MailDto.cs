namespace MailSender.Application.DTOs.Mail;

public record MailDto(
    string To,
    string Subject,
    string Body
);