namespace MailSender.Infrastructure.MailProviders;

public class MailtrapSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string SenderEmail { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public bool UseSandbox { get; set; }
    public int InboxId { get; set; }
}
