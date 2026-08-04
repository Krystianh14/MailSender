using FluentAssertions;
using MailSender.Application.Interfaces;
using MailSender.Application.Services;
using MailSender.Domain.Entities;
using NSubstitute;
using Xunit;

namespace MailSender.UnitTests.Services;

public class MailLogServiceTests
{
    private readonly IMailLogRepository _mailLogRepository;
    private readonly MailLogService _service;

    public MailLogServiceTests()
    {
        _mailLogRepository = Substitute.For<IMailLogRepository>();
        _service = new MailLogService(_mailLogRepository);
    }

    [Fact]
    public async Task GetLogsAsync_ReturnsMappedLogsForSelectedApplication()
    {
        // Arrange
        var application = CreateApplication();

        var successLog = MailSendLog.Success(
            application,
            "first-recipient@example.com",
            "First subject",
            "First message body");

        var failedLog = MailSendLog.Failed(
            application,
            "second-recipient@example.com",
            "Second subject",
            "Second message body",
            "Provider unavailable");

        var logs = new List<MailSendLog>
        {
            successLog,
            failedLog
        };

        _mailLogRepository
            .GetByClientApplicationIdAsync(application.Id)
            .Returns(logs);

        // Act
        var result = await _service.GetLogsAsync(application);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Error.Should().BeNull();
        result.Data.Should().NotBeNull();
        result.Data.Should().HaveCount(2);

        var firstLog = result.Data![0];

        firstLog.Id.Should().Be(successLog.Id);
        firstLog.AppId.Should().Be(successLog.AppId);
        firstLog.AppName.Should().Be(successLog.AppName);
        firstLog.To.Should().Be(successLog.To);
        firstLog.Subject.Should().Be(successLog.Subject);
        firstLog.Body.Should().Be(successLog.Body);
        firstLog.Status.Should().Be("Success");
        firstLog.ErrorMessage.Should().BeNull();
        firstLog.CreatedAtUtc.Should().Be(successLog.CreatedAtUtc);

        var secondLog = result.Data[1];

        secondLog.Id.Should().Be(failedLog.Id);
        secondLog.AppId.Should().Be(failedLog.AppId);
        secondLog.AppName.Should().Be(failedLog.AppName);
        secondLog.To.Should().Be(failedLog.To);
        secondLog.Subject.Should().Be(failedLog.Subject);
        secondLog.Body.Should().Be(failedLog.Body);
        secondLog.Status.Should().Be("Failed");
        secondLog.ErrorMessage.Should().Be(failedLog.ErrorMessage);
        secondLog.CreatedAtUtc.Should().Be(failedLog.CreatedAtUtc);

        await _mailLogRepository
            .Received(1)
            .GetByClientApplicationIdAsync(application.Id);
    }

    [Fact]
    public async Task GetLogByIdAsync_WhenLogExists_ReturnsLog()
    {
        // Arrange
        var application = CreateApplication();

        var log = MailSendLog.Success(
            application,
            "recipient@example.com",
            "Test subject",
            "Test message body");

        _mailLogRepository
            .GetByIdAndClientApplicationIdAsync(
                log.Id,
                application.Id)
            .Returns(log);

        // Act
        var result = await _service.GetLogByIdAsync(
            log.Id,
            application);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Error.Should().BeNull();
        result.Data.Should().NotBeNull();

        result.Data!.Id.Should().Be(log.Id);
        result.Data.AppId.Should().Be(log.AppId);
        result.Data.AppName.Should().Be(log.AppName);
        result.Data.To.Should().Be(log.To);
        result.Data.Subject.Should().Be(log.Subject);
        result.Data.Body.Should().Be(log.Body);
        result.Data.Status.Should().Be(log.Status);
        result.Data.ErrorMessage.Should().Be(log.ErrorMessage);
        result.Data.CreatedAtUtc.Should().Be(log.CreatedAtUtc);

        await _mailLogRepository
            .Received(1)
            .GetByIdAndClientApplicationIdAsync(
                log.Id,
                application.Id);
    }

    [Fact]
    public async Task GetLogByIdAsync_WhenLogDoesNotExist_ReturnsFailure()
    {
        // Arrange
        var application = CreateApplication();
        var missingLogId = Guid.NewGuid();

        _mailLogRepository
            .GetByIdAndClientApplicationIdAsync(
                missingLogId,
                application.Id)
            .Returns((MailSendLog?)null);

        // Act
        var result = await _service.GetLogByIdAsync(
            missingLogId,
            application);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Data.Should().BeNull();
        result.Error.Should().Be("Mail log not found.");

        await _mailLogRepository
            .Received(1)
            .GetByIdAndClientApplicationIdAsync(
                missingLogId,
                application.Id);
    }

    [Fact]
    public async Task GetLogByIdAsync_WhenLogBelongsToAnotherApplication_ReturnsFailure()
    {
        // Arrange
        var requestingApplication = new ClientApplication(
            "requesting-app-id",
            "Requesting Application");

        var owningApplication = new ClientApplication(
            "owning-app-id",
            "Owning Application");

        var foreignLog = MailSendLog.Success(
            owningApplication,
            "recipient@example.com",
            "Private subject",
            "Private message body");

        _mailLogRepository
            .GetByIdAndClientApplicationIdAsync(
                foreignLog.Id,
                requestingApplication.Id)
            .Returns((MailSendLog?)null);

        // Act
        var result = await _service.GetLogByIdAsync(
            foreignLog.Id,
            requestingApplication);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Data.Should().BeNull();
        result.Error.Should().Be("Mail log not found.");

        await _mailLogRepository
            .Received(1)
            .GetByIdAndClientApplicationIdAsync(
                foreignLog.Id,
                requestingApplication.Id);

        await _mailLogRepository
            .DidNotReceive()
            .GetByIdAndClientApplicationIdAsync(
                foreignLog.Id,
                owningApplication.Id);
    }

    private static ClientApplication CreateApplication()
    {
        return new ClientApplication(
            "test-app-id",
            "Test Application");
    }
}