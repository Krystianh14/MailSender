using MailSender.Application.Common;
using MailSender.Application.DTOs.Mail;
using MailSender.Application.Interfaces;
using MailSender.Application.Settings;
using MailSender.Domain.Entities;
using Microsoft.Extensions.Options;

namespace MailSender.Application.Services;

public class MailService
{
    private const string QuestionPrefix = "[Q] ";
    private const string StudentSurnameMarker = "[student.surname]";

    private readonly IMailSenderProvider _mailSenderProvider;
    private readonly List<StudentSettings> _students;

    public MailService(IMailSenderProvider mailSenderProvider, IOptions<List<StudentSettings>> studentOptions)
    {
        _mailSenderProvider = mailSenderProvider;
        _students = studentOptions.Value;
    }

    public async Task<ServiceResult<SendMailResponse>> SendAsync(SendMailRequest request, ClientApplication application)
    {
        var subject = PrepareSubject(request.Subject);
        var body = PrepareBody(request.Body);

        var message = new MailMessage(
            request.To,
            subject,
            body
        );

        await _mailSenderProvider.SendAsync(message);

        var emailDto = new MailDto(
            message.To,
            message.Subject,
            message.Body
        );

        var response = new SendMailResponse(
            application.AppId,
            application.AppName,
            "queued",
            emailDto
        );

        return ServiceResult<SendMailResponse>.Success(response);
    }

    private static string PrepareSubject(string subject)
    {
        if (subject.TrimEnd().EndsWith("?"))
        {
            return $"{QuestionPrefix}{subject}";
        }

        return subject;
    }

    private string PrepareBody(string body)
{
    var result = body;

    foreach (var student in _students)
    {
        if (string.IsNullOrWhiteSpace(student.Surname))
        {
            continue;
        }

        if (!result.Contains(student.Surname))
        {
            continue;
        }

        result = result.Replace(
            student.Surname,
            $"{StudentSurnameMarker}{student.Surname}{StudentSurnameMarker}"
        );
    }

    return result;
}
}