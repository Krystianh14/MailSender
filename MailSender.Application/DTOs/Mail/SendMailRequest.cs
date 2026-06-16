namespace MailSender.Application.DTOs.Mail;

public record SendMailRequest(
    string To,
    string Subject,
    string Body
);