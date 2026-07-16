namespace MailSender.Domain.Entities;

public class ClientApplication
{
    public Guid Id { get; private set; }
    public string AppId { get; private set; } = string.Empty;
    public string AppName { get; private set; } = string.Empty;

    private ClientApplication()
    {
        
    }

    public ClientApplication(string appId, string appName)
    {
        if (string.IsNullOrWhiteSpace(appId))
        {
            throw new ArgumentException("AppId cannot be empty.", nameof(appId));
        }

        if (string.IsNullOrWhiteSpace(appName))
        {
            throw new ArgumentException("AppName cannot be empty.", nameof(appName));
        }

        Id = Guid.NewGuid();
        AppId = appId;
        AppName = appName;
    }
}