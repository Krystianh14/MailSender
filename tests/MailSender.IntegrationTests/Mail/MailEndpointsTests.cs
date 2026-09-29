using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using MailSender.Application.DTOs.Mail;
using MailSender.IntegrationTests.Infrastructure;

namespace MailSender.IntegrationTests.Mail;

public class MailEndpointsTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("not-a-jwt")]
    public async Task Send_WhenTokenIsMissingOrMalformed_ReturnsUnauthorized(string? token)
    {
        // Arrange
        using var factory = new MailSenderApiFactory();
        using var client = factory.CreateApiClient();
        if (token is not null)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        // Act
        using var response = await client.PostAsJsonAsync("/mail/send", CreateRequest());

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains(response.Headers.WwwAuthenticate, header => header.Scheme == "Bearer");
        Assert.Empty(factory.MailProvider.Messages);
        await client.RegisterAndAuthenticateAsync();
        Assert.Empty(await client.GetLogsAsync());
    }

    [Fact]
    public async Task Send_WhenTokenSignatureIsChanged_ReturnsUnauthorized()
    {
        // Arrange
        using var factory = new MailSenderApiFactory();
        using var client = factory.CreateApiClient();
        var registration = await client.RegisterAsync();
        var parts = registration.Key.Split('.');
        parts[2] = (parts[2][0] == 'A' ? "B" : "A") + parts[2][1..];
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", string.Join('.', parts));

        // Act
        using var response = await client.PostAsJsonAsync("/mail/send", CreateRequest());

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Empty(factory.MailProvider.Messages);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", registration.Key);
        Assert.Empty(await client.GetLogsAsync());
    }

    [Fact]
    public async Task Send_WhenSignedTokenReferencesUnknownClient_ReturnsUnauthorized()
    {
        // Arrange
        using var issuingFactory = new MailSenderApiFactory();
        using var issuingClient = issuingFactory.CreateApiClient();
        var registration = await issuingClient.RegisterAsync();
        using var factory = new MailSenderApiFactory();
        using var client = factory.CreateApiClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", registration.Key);

        // Act
        using var response = await client.PostAsJsonAsync("/mail/send", CreateRequest());

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        await response.AssertErrorAsync("Client application not found.");
        Assert.Empty(factory.MailProvider.Messages);
    }

    [Theory]
    [InlineData("Test subject", "Hello", "Test subject", "Hello")]
    [InlineData("Ready?", "Hello Testowski", "[Q] Ready?", "Hello [student.surname]Testowski[student.surname]")]
    public async Task Send_WhenRequestIsValid_ReturnsDeliveredMessageAndPersistsLog(
        string subject, string body, string expectedSubject, string expectedBody)
    {
        // Arrange
        using var factory = new MailSenderApiFactory();
        using var client = factory.CreateApiClient();
        var registration = await client.RegisterAndAuthenticateAsync();
        var request = new SendMailRequest("recipient@example.com", subject, body);
        var beforeSend = DateTime.UtcNow;

        // Act
        using var response = await client.PostAsJsonAsync("/mail/send", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.ReadRequiredAsync<SendMailResponse>();
        Assert.Equal(registration.AppId, result.AppId);
        Assert.Equal(registration.AppName, result.AppName);
        Assert.Equal("Success", result.Status);
        Assert.Equal(new MailDto(request.To, expectedSubject, expectedBody), result.Email);
        var delivered = Assert.Single(factory.MailProvider.Messages);
        Assert.Equal(result.Email, new MailDto(delivered.To, delivered.Subject, delivered.Body));
        var log = Assert.Single(await client.GetLogsAsync());
        Assert.NotEqual(Guid.Empty, log.Id);
        Assert.Equal(registration.AppId, log.AppId);
        Assert.Equal(registration.AppName, log.AppName);
        Assert.Equal(result.Email, new MailDto(log.To, log.Subject, log.Body));
        Assert.Equal("Success", log.Status);
        Assert.Null(log.ErrorMessage);
        Assert.InRange(log.CreatedAtUtc, beforeSend, DateTime.UtcNow);
    }

    [Fact]
    public async Task Send_WhenProviderFails_ReturnsBadRequestAndPersistsFailedLog()
    {
        // Arrange
        using var factory = new MailSenderApiFactory();
        factory.MailProvider.FailureMessage = "Provider unavailable";
        using var client = factory.CreateApiClient();
        var registration = await client.RegisterAndAuthenticateAsync();
        var request = new SendMailRequest("recipient@example.com", "Ready?", "Hello Testowski");

        // Act
        using var response = await client.PostAsJsonAsync("/mail/send", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await response.AssertErrorAsync("Email sending failed: Provider unavailable");
        var attempted = Assert.Single(factory.MailProvider.Messages);
        Assert.Equal("[Q] Ready?", attempted.Subject);
        Assert.Equal("Hello [student.surname]Testowski[student.surname]", attempted.Body);
        var log = Assert.Single(await client.GetLogsAsync());
        Assert.Equal(registration.AppId, log.AppId);
        Assert.Equal(registration.AppName, log.AppName);
        Assert.Equal(request.To, log.To);
        Assert.Equal(attempted.Subject, log.Subject);
        Assert.Equal(attempted.Body, log.Body);
        Assert.Equal("Failed", log.Status);
        Assert.Equal("Provider unavailable", log.ErrorMessage);
    }

    [Theory]
    [InlineData("to")]
    [InlineData("subject")]
    [InlineData("body")]
    public async Task Send_WhenRequiredFieldIsMissing_ReturnsBadRequestWithoutDelivery(string missingField)
    {
        // Arrange
        using var factory = new MailSenderApiFactory();
        using var client = factory.CreateApiClient();
        await client.RegisterAndAuthenticateAsync();
        var request = new Dictionary<string, string>
        {
            ["to"] = "recipient@example.com",
            ["subject"] = "Test subject",
            ["body"] = "Test body"
        };
        request.Remove(missingField);

        // Act
        using var response = await client.PostAsJsonAsync("/mail/send", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(factory.MailProvider.Messages);
        Assert.Empty(await client.GetLogsAsync());
    }

    private static SendMailRequest CreateRequest()
    {
        return new SendMailRequest("recipient@example.com", "Test subject", "Test message body");
    }
}
