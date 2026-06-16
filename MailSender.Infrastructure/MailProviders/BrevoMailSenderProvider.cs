using System.Net.Http.Json;
using MailSender.Application.Interfaces;
using MailSender.Domain.Entities;
using Microsoft.Extensions.Options;

namespace MailSender.Infrastructure.MailProviders;

public class BrevoMailSenderProvider : IMailSenderProvider
{
    private readonly HttpClient _httpClient;
    private readonly BrevoSettings _settings;

    public BrevoMailSenderProvider(
        HttpClient httpClient,
        IOptions<BrevoSettings> options)
    {
        _httpClient = httpClient;
        _settings = options.Value;
    }

    public async Task SendAsync(MailMessage message)
    {
        var requestBody = new
        {
            sender = new
            {
                name = _settings.SenderName,
                email = _settings.SenderEmail
            },
            to = new[]
            {
                new
                {
                    email = message.To
                }
            },
            subject = message.Subject,
            htmlContent = message.Body
        };

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "https://api.brevo.com/v3/smtp/email"
        );

        request.Headers.Add("api-key", _settings.ApiKey);
        request.Content = JsonContent.Create(requestBody);

        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();

            throw new InvalidOperationException(
                $"Brevo email sending failed. StatusCode: {response.StatusCode}. Response: {errorContent}"
            );
        }
    }
}