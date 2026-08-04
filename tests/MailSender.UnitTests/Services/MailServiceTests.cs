using FluentAssertions;
using MailSender.Application.DTOs.Mail;
using MailSender.Application.Interfaces;
using MailSender.Application.Services;
using MailSender.Application.Settings;
using MailSender.Domain.Entities;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace MailSender.UnitTests.Services;

public class MailServiceTests
{
    private readonly IMailSenderProvider _mailSenderProvider;
    private readonly IMailLogRepository _mailLogRepository;

    public MailServiceTests()
    {
        _mailSenderProvider = Substitute.For<IMailSenderProvider>();
        _mailLogRepository = Substitute.For<IMailLogRepository>();
    }

    [Fact]
    public async Task SendAsync_WhenSubjectEndsWithQuestionMark_AddsQuestionPrefix()
    {
        // Arrange
        const string expectedSubject = "[Q] Czy testy działają?";

        var service = CreateService();

        var request = new SendMailRequest(
            "recipient@example.com",
            "Czy testy działają?",
            "Test message body");

        var application = CreateApplication();

        // Act
        await service.SendAsync(request, application);

        // Assert
        await _mailSenderProvider
            .Received(1)
            .SendAsync(Arg.Is<MailMessage>(message =>
                message != null &&
                message.Subject == expectedSubject));
    }

    [Fact]
    public async Task SendAsync_WhenBodyContainsConfiguredSurname_AddsMarker()
    {
        // Arrange
        const string surname = "Haberka";
        const string expectedBody =
            $"Test message from [student.surname]{surname}[student.surname]";

        var students = new List<StudentSettings>
        {
            new()
            {
                Surname = surname,
                IndexSuffix = "13"
            }
        };

        var service = CreateService(students);

        var request = new SendMailRequest(
            "recipient@example.com",
            "Test subject",
            $"Test message from {surname}");

        var application = CreateApplication();

        // Act
        await service.SendAsync(request, application);

        // Assert
        await _mailSenderProvider
            .Received(1)
            .SendAsync(Arg.Is<MailMessage>(message =>
                message != null &&
                message.Body == expectedBody));
    }

    [Fact]
    public async Task SendAsync_WhenProviderSucceeds_ReturnsSuccess()
    {
        // Arrange
        var service = CreateService();
        var request = CreateRequest();
        var application = CreateApplication();

        // Act
        var result = await service.SendAsync(request, application);
        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Error.Should().BeNull();

        result.Data.Should().NotBeNull();
        result.Data!.AppId.Should().Be(application.AppId);
        result.Data.AppName.Should().Be(application.AppName);
        result.Data.Status.Should().Be("Success");

        result.Data.Email.To.Should().Be(request.To);
        result.Data.Email.Subject.Should().Be(request.Subject);
        result.Data.Email.Body.Should().Be(request.Body);
    }

    [Fact]
    public async Task SendAsync_WhenProviderSucceeds_SavesSuccessLog()
    {
        // Arrange
        var service = CreateService();
        var request = CreateRequest();
        var application = CreateApplication();

        // Act
        await service.SendAsync(request, application);
        // Assert
        await _mailLogRepository
            .Received(1)
            .AddAsync(Arg.Is<MailSendLog>(log =>
                log != null &&
                log.ClientApplicationId == application.Id &&
                log.AppId == application.AppId &&
                log.AppName == application.AppName &&
                log.To == request.To &&
                log.Subject == request.Subject &&
                log.Body == request.Body &&
                log.Status == "Success" &&
                log.ErrorMessage == null));
    }

    [Fact]
    public async Task SendAsync_WhenProviderFails_ReturnsFailure()
    {
        // Arrange
        const string providerError = "Provider unavailable";

        var service = CreateService();

        var request = CreateRequest();
        var application = CreateApplication();

        _mailSenderProvider
            .SendAsync(Arg.Any<MailMessage>())
            .Returns(_ => Task.FromException(
                new InvalidOperationException(providerError)));

        // Act
        var result = await service.SendAsync(request, application);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Data.Should().BeNull();
        result.Error.Should()
            .Be($"Email sending failed: {providerError}");
    }

    [Fact]
    public async Task SendAsync_WhenProviderFails_SavesFailedLog()
    {
        // Arrange
        const string providerError = "Provider unavailable";

        var service = CreateService();

        var request = CreateRequest();
        var application = CreateApplication();

        _mailSenderProvider
            .SendAsync(Arg.Any<MailMessage>())
            .Returns(_ => Task.FromException(
                new InvalidOperationException(providerError)));

        // Act
        await service.SendAsync(request, application);

        // Assert
        await _mailLogRepository
            .Received(1)
            .AddAsync(Arg.Is<MailSendLog>(log =>
                log != null &&
                log.ClientApplicationId == application.Id &&
                log.AppId == application.AppId &&
                log.AppName == application.AppName &&
                log.To == request.To &&
                log.Subject == request.Subject &&
                log.Body == request.Body &&
                log.Status == "Failed" &&
                log.ErrorMessage == providerError));
    }

    [Fact]
    public async Task SendAsync_PassesProcessedMessageToProvider()
    {
       // Arrange
        const string surname = "Haberka";
        const string expectedSubject = "[Q] Czy testy działają?";
        const string expectedBody =
            "Message from [student.surname]Haberka[student.surname]";

        var students = new List<StudentSettings>
        {
            new()
            {
                Surname = surname,
                IndexSuffix = "13"
            }
        };

        var service = CreateService(students);

        var request = new SendMailRequest(
            "recipient@example.com",
            "Czy testy działają?",
            $"Message from {surname}");

        var application = CreateApplication();

        // Act
        await service.SendAsync(request, application);

        // Assert
        await _mailSenderProvider
            .Received(1)
            .SendAsync(Arg.Is<MailMessage>(message =>
                message != null &&
                message.To == request.To &&
                message.Subject == expectedSubject &&
                message.Body == expectedBody));
    }

    private MailService CreateService(
        List<StudentSettings>? students = null)
    {
        var options = Options.Create(
            students ?? new List<StudentSettings>());

        return new MailService(
            _mailSenderProvider,
            _mailLogRepository,
            options);
    }

    private static SendMailRequest CreateRequest()
    {
        return new SendMailRequest(
            "recipient@example.com",
            "Test subject",
            "Test message body");
    }

    private static ClientApplication CreateApplication()
    {
        return new ClientApplication(
            "test-app-id",
            "Test Application");
    }
}