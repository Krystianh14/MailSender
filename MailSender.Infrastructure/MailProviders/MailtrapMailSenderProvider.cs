using System.Net.Http.Json;
using MailSender.Application.Interfaces;
using MailSender.Domain.Entities;
using Microsoft.Extensions.Options;

namespace MailSender.Infrastructure.MailProviders;

public class MailtrapMailSenderProvider : IMailSenderProvider
{
    private readonly HttpClient _httpClient;
    private readonly MailtrapSettings _settings;

    public MailtrapMailSenderProvider(
        HttpClient httpClient,
        IOptions<MailtrapSettings> options)
    {
        _httpClient = httpClient;
        _settings = options.Value;
    }

    public async Task SendAsync(MailMessage message)
    {
        ValidateSettings();

        var requestBody = new
        {
            from = new
            {
                email = _settings.SenderEmail,
                name = _settings.SenderName
            },
            to = new[]
            {
                new
                {
                    email = message.To
                }
            },
            subject = message.Subject,
            text = message.Body,
            html = message.Body
        };

        var url = GetRequestUrl();

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("Authorization", $"Bearer {_settings.ApiKey}");
        request.Content = JsonContent.Create(requestBody);

        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();

            throw new InvalidOperationException(
                $"Mailtrap email sending failed. StatusCode: {response.StatusCode}. Response: {errorContent}"
            );
        }
    }

    private string GetRequestUrl()
    {
        if (_settings.UseSandbox)
        {
            return $"https://sandbox.api.mailtrap.io/api/send/{_settings.InboxId}";
        }

        return "https://send.api.mailtrap.io/api/send";
    }

    private void ValidateSettings()
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            throw new InvalidOperationException("Mailtrap ApiKey is missing.");
        }

        if (string.IsNullOrWhiteSpace(_settings.SenderEmail))
        {
            throw new InvalidOperationException("Mailtrap SenderEmail is missing.");
        }

        if (string.IsNullOrWhiteSpace(_settings.SenderName))
        {
            throw new InvalidOperationException("Mailtrap SenderName is missing.");
        }

        if (_settings.UseSandbox && _settings.InboxId <= 0)
        {
            throw new InvalidOperationException("Mailtrap InboxId is required when UseSandbox is true.");
        }
    }
}
