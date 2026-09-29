using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using MailSender.Application.DTOs.ClientApps;
using MailSender.IntegrationTests.Infrastructure;

namespace MailSender.IntegrationTests.ClientApplications;

public class ClientApplicationEndpointsTests
{
    [Fact]
    public async Task Register_WhenRequestIsValid_ReturnsSuccess()
    {
        // Arrange
        using var factory = new MailSenderApiFactory();
        using var client = factory.CreateApiClient();
        var request = new RegisterClientAppRequest("test-app", "Test Application",
            MailSenderApiFactory.RegistrationPassword);

        // Act
        using var response = await client.PostAsJsonAsync("/client-app/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.ReadRequiredAsync<RegisterClientAppResponse>();
        Assert.Equal(request.AppId, result.AppId);
        Assert.Equal(request.AppName, result.AppName);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.Key);
        Assert.Equal(request.AppId, token.Claims.Single(c => c.Type == "app_id").Value);
        Assert.Equal(request.AppName, token.Claims.Single(c => c.Type == "app_name").Value);
        Assert.True(Guid.TryParse(token.Claims.Single(c => c.Type == "client_application_id").Value, out var id));
        Assert.NotEqual(Guid.Empty, id);
        // The real bearer handler must accept the token and resolve the persisted client.
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.Key);
        Assert.Empty(await client.GetLogsAsync());
    }

    [Fact]
    public async Task Register_WhenPasswordIsInvalid_ReturnsForbiddenWithoutSavingClient()
    {
        // Arrange
        using var factory = new MailSenderApiFactory();
        using var client = factory.CreateApiClient();
        var request = new RegisterClientAppRequest("test-app", "Test Application", "invalid-password");

        // Act
        using var response = await client.PostAsJsonAsync("/client-app/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        await response.AssertErrorAsync("Invalid index-based password. Expected one of suffixes: 42");
        await client.RegisterAsync(request.AppId, request.AppName);
    }

    [Fact]
    public async Task Register_WhenAppIdAlreadyExists_ReturnsForbidden()
    {
        // Arrange
        using var factory = new MailSenderApiFactory();
        using var client = factory.CreateApiClient();
        await client.RegisterAsync();
        var request = new RegisterClientAppRequest("test-app", "Other Application",
            MailSenderApiFactory.RegistrationPassword);

        // Act
        using var response = await client.PostAsJsonAsync("/client-app/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        await response.AssertErrorAsync("client app duplication. Existing test-app Test Application");
        await client.RegisterAsync("other-app", request.AppName);
    }

    [Fact]
    public async Task Register_WhenAppNameAlreadyExists_ReturnsForbidden()
    {
        // Arrange
        using var factory = new MailSenderApiFactory();
        using var client = factory.CreateApiClient();
        await client.RegisterAsync();
        var request = new RegisterClientAppRequest("other-app", "Test Application",
            MailSenderApiFactory.RegistrationPassword);

        // Act
        using var response = await client.PostAsJsonAsync("/client-app/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        await response.AssertErrorAsync("client app duplication. Existing test-app Test Application");
        await client.RegisterAsync(request.AppId, "Other Application");
    }

    [Fact]
    public async Task Register_WhenFactoriesUseSameClientDetails_KeepsDatabasesIsolated()
    {
        // Arrange
        using var firstFactory = new MailSenderApiFactory();
        using var secondFactory = new MailSenderApiFactory();
        using var firstClient = firstFactory.CreateApiClient();
        using var secondClient = secondFactory.CreateApiClient();
        await firstClient.RegisterAndAuthenticateAsync();
        await firstClient.SendMailAsync();

        // Act
        await secondClient.RegisterAndAuthenticateAsync();

        // Assert
        Assert.Single(await firstClient.GetLogsAsync());
        Assert.Empty(await secondClient.GetLogsAsync());
        Assert.Empty(secondFactory.MailProvider.Messages);
    }
}
