using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using MailSender.Application.DTOs.ClientApps;
using MailSender.Application.DTOs.Mail;
using MailSender.Application.DTOs.MailLogs;

namespace MailSender.IntegrationTests.Infrastructure;

internal static class ApiTestHelpers
{
    public static async Task<RegisterClientAppResponse> RegisterAsync(
        this HttpClient client, string appId = "test-app", string appName = "Test Application")
    {
        using var response = await client.PostAsJsonAsync("/client-app/register",
            new RegisterClientAppRequest(appId, appName, MailSenderApiFactory.RegistrationPassword));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.ReadRequiredAsync<RegisterClientAppResponse>();
    }

    public static async Task<RegisterClientAppResponse> RegisterAndAuthenticateAsync(
        this HttpClient client, string appId = "test-app", string appName = "Test Application")
    {
        var registration = await client.RegisterAsync(appId, appName);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", registration.Key);
        return registration;
    }

    public static async Task<T> ReadRequiredAsync<T>(this HttpResponseMessage response)
    {
        var result = await response.Content.ReadFromJsonAsync<T>();
        Assert.NotNull(result);
        return result;
    }

    public static async Task AssertErrorAsync(this HttpResponseMessage response, string expected)
    {
        var body = await response.ReadRequiredAsync<JsonElement>();
        Assert.Equal(expected, body.GetProperty("error").GetString());
    }

    public static async Task<List<MailLogDto>> GetLogsAsync(this HttpClient client)
    {
        using var response = await client.GetAsync("/mail-log");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.ReadRequiredAsync<List<MailLogDto>>();
    }

    public static async Task SendMailAsync(this HttpClient client, string subject = "Test subject")
    {
        using var response = await client.PostAsJsonAsync("/mail/send",
            new SendMailRequest("recipient@example.com", subject, "Test message body"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
