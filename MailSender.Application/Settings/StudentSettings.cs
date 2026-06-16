namespace MailSender.Application.Settings;

public class StudentSettings
{
    public string Surname { get; set; } = string.Empty;
    public string IndexSuffix { get; set; } = string.Empty;

    public string Password => $"dwa{IndexSuffix}";
}