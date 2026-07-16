namespace MailSender.Domain.Entities;

public class MailSendLog
{
    public Guid Id { get; private set; }
    public Guid ClientApplicationId { get; private set; }

    public string AppId { get; private set; } = string.Empty;
    public string AppName { get; private set; } = string.Empty;

    public string To { get; private set; } = string.Empty;
    public string Subject { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;

    public string Status { get; private set; } = string.Empty;
    public string? ErrorMessage { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    private MailSendLog()
    {
        
    }

    private MailSendLog(ClientApplication application, string to, string subject, string body, string status, string? errorMessage)
    {
        Id = Guid.NewGuid();

        ClientApplicationId = application.Id;
        AppId = application.AppId;
        AppName = application.AppName;

        To = to;
        Subject = subject;
        Body = body;

        Status = status;
        ErrorMessage = errorMessage;

        CreatedAtUtc = DateTime.UtcNow;
    }

    public static MailSendLog Success(ClientApplication application, string to, string subject, string body)
    {
        return new MailSendLog(application, to, subject, body, "Success", null);
    }

    public static MailSendLog Failed(ClientApplication application, string to, string subject, string body, string errorMessage)
    {
        return new MailSendLog(application, to, subject, body, "Failed", errorMessage);
    }
}