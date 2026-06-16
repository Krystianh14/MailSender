namespace MailSender.Infrastructure.MailProviders;

public class BrevoSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string SenderEmail { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
}