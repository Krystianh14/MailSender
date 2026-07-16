namespace MailSender.Domain.Entities;


public class MailMessage
{
    public string To { get; private set; }
    public string Subject { get; private set; }
    public string Body { get; private set; }

    public MailMessage(string to, string subject, string body)
    {
        if (string.IsNullOrWhiteSpace(to))
        {
            throw new ArgumentException("Recipient email cannot be empty.", nameof(to));
        }

        if (string.IsNullOrWhiteSpace(subject))
        {
            throw new ArgumentException("Subject cannot be empty.", nameof(subject));
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            throw new ArgumentException("Body cannot be empty.", nameof(body));
        }

        To = to;
        Subject = subject;
        Body = body;
    }
}

