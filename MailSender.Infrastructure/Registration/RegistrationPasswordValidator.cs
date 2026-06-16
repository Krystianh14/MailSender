using MailSender.Application.Interfaces;
using Microsoft.Extensions.Options;

namespace MailSender.Infrastructure.Registration;

public class RegistrationPasswordValidator : IRegistrationPasswordValidator
{
    private readonly RegistrationSettings _settings;

    public RegistrationPasswordValidator(IOptions<RegistrationSettings> options)
    {
        _settings = options.Value;
    }

    public bool IsValid(string password)
    {
        return password == _settings.Password;
    }

    public string GetInvalidPasswordMessage()
    {
        return $"Invalid index-based password {_settings.IndexSuffix}";
    }
}