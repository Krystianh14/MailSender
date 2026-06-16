using MailSender.Application.Interfaces;
using MailSender.Application.Settings;
using Microsoft.Extensions.Options;

namespace MailSender.Infrastructure.Registration;

public class RegistrationPasswordValidator : IRegistrationPasswordValidator
{
    private readonly List<StudentSettings> _students;

    public RegistrationPasswordValidator(IOptions<List<StudentSettings>> options)
    {
        _students = options.Value;
    }

    public bool IsValid(string password)
    {
        return _students.Any(student => student.Password == password);
    }

    public string GetInvalidPasswordMessage()
    {
        var suffixes = string.Join(", ", _students.Select(student => student.IndexSuffix));

        return $"Invalid index-based password. Expected one of suffixes: {suffixes}";
    }
}