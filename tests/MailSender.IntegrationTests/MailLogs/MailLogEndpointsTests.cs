using System.Net;
using System.Net.Http.Headers;
using MailSender.Application.DTOs.MailLogs;
using MailSender.IntegrationTests.Infrastructure;

namespace MailSender.IntegrationTests.MailLogs;

public class MailLogEndpointsTests
{
    [Theory]
    [InlineData("/mail-log")]
    [InlineData("/mail-log/11111111-1111-1111-1111-111111111111")]
    public async Task GetLogs_WhenTokenIsMissing_ReturnsUnauthorized(string route)
    {
        // Arrange
        using var factory = new MailSenderApiFactory();
        using var client = factory.CreateApiClient();

        // Act
        using var response = await client.GetAsync(route);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains(response.Headers.WwwAuthenticate, header => header.Scheme == "Bearer");
    }

    [Theory]
    [InlineData("/mail-log")]
    [InlineData("/mail-log/11111111-1111-1111-1111-111111111111")]
    public async Task GetLogs_WhenSignedTokenReferencesUnknownClient_ReturnsUnauthorized(string route)
    {
        // Arrange
        using var issuingFactory = new MailSenderApiFactory();
        using var issuingClient = issuingFactory.CreateApiClient();
        var registration = await issuingClient.RegisterAsync();
        using var factory = new MailSenderApiFactory();
        using var client = factory.CreateApiClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", registration.Key);

        // Act
        using var response = await client.GetAsync(route);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        await response.AssertErrorAsync("Client application not found.");
    }

    [Fact]
    public async Task GetLogs_WhenClientHasNoLogs_ReturnsEmptyArray()
    {
        // Arrange
        using var factory = new MailSenderApiFactory();
        using var client = factory.CreateApiClient();
        await client.RegisterAndAuthenticateAsync();

        // Act
        using var response = await client.GetAsync("/mail-log");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(await response.ReadRequiredAsync<List<MailLogDto>>());
    }

    [Fact]
    public async Task GetLogs_WhenMultipleClientsHaveLogs_ReturnsOnlyOwnLogsNewestFirst()
    {
        // Arrange
        using var factory = new MailSenderApiFactory();
        using var client = factory.CreateApiClient();
        using var otherClient = factory.CreateApiClient();
        var registration = await client.RegisterAndAuthenticateAsync();
        await otherClient.RegisterAndAuthenticateAsync("other-app", "Other Application");
        await client.SendMailAsync("First message");
        await otherClient.SendMailAsync("Private message");
        await client.SendMailAsync("Second message");

        // Act
        using var response = await client.GetAsync("/mail-log");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var logs = await response.ReadRequiredAsync<List<MailLogDto>>();
        Assert.Equal(2, logs.Count);
        Assert.Equal(2, logs.Select(log => log.Id).Distinct().Count());
        Assert.Contains(logs, log => log.Subject == "First message");
        Assert.Contains(logs, log => log.Subject == "Second message");
        Assert.True(logs[0].CreatedAtUtc >= logs[1].CreatedAtUtc);
        Assert.All(logs, log =>
        {
            Assert.Equal(registration.AppId, log.AppId);
            Assert.Equal(registration.AppName, log.AppName);
            Assert.Equal("recipient@example.com", log.To);
            Assert.Equal("Test message body", log.Body);
            Assert.Equal("Success", log.Status);
            Assert.Null(log.ErrorMessage);
        });
        Assert.Equal("Private message", Assert.Single(await otherClient.GetLogsAsync()).Subject);
    }

    [Fact]
    public async Task GetLogById_WhenLogBelongsToClient_ReturnsMatchingLog()
    {
        // Arrange
        using var factory = new MailSenderApiFactory();
        using var client = factory.CreateApiClient();
        await client.RegisterAndAuthenticateAsync();
        await client.SendMailAsync();
        var expected = Assert.Single(await client.GetLogsAsync());

        // Act
        using var response = await client.GetAsync($"/mail-log/{expected.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(expected, await response.ReadRequiredAsync<MailLogDto>());
    }

    [Fact]
    public async Task GetLogById_WhenLogDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        using var factory = new MailSenderApiFactory();
        using var client = factory.CreateApiClient();
        await client.RegisterAndAuthenticateAsync();

        // Act
        using var response = await client.GetAsync($"/mail-log/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await response.AssertErrorAsync("Mail log not found.");
    }

    [Fact]
    public async Task GetLogById_WhenLogBelongsToAnotherClient_ReturnsNotFound()
    {
        // Arrange
        using var factory = new MailSenderApiFactory();
        using var owner = factory.CreateApiClient();
        using var otherClient = factory.CreateApiClient();
        await owner.RegisterAndAuthenticateAsync();
        await otherClient.RegisterAndAuthenticateAsync("other-app", "Other Application");
        await owner.SendMailAsync("Private message");
        var privateLog = Assert.Single(await owner.GetLogsAsync());

        // Act
        using var response = await otherClient.GetAsync($"/mail-log/{privateLog.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await response.AssertErrorAsync("Mail log not found.");
        Assert.Empty(await otherClient.GetLogsAsync());
        using var ownerResponse = await owner.GetAsync($"/mail-log/{privateLog.Id}");
        Assert.Equal(HttpStatusCode.OK, ownerResponse.StatusCode);
        Assert.Equal(privateLog, await ownerResponse.ReadRequiredAsync<MailLogDto>());
    }
}
