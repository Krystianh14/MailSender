using MailSender.Application.DTOs.Mail;
using MailSender.Application.Interfaces;
using MailSender.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MailSender.Api.Controllers;

[ApiController]
[Route("mail")]
public class MailController : ControllerBase
{
    private readonly MailService _mailService;
    private readonly IClientApplicationRepository _clientApplicationRepository;

    public MailController(
        MailService mailService,
        IClientApplicationRepository clientApplicationRepository)
    {
        _mailService = mailService;
        _clientApplicationRepository = clientApplicationRepository;
    }

    [Authorize]
    [HttpPost("send")]
    public async Task<IActionResult> Send(SendMailRequest request)
    {
        var appId = User.FindFirst("app_id")?.Value;

        if (string.IsNullOrWhiteSpace(appId))
        {
            return Unauthorized(new
            {
                error = "Invalid token. Missing app_id claim."
            });
        }

        var application = await _clientApplicationRepository.GetByAppIdAsync(appId);

        if (application is null)
        {
            return Unauthorized(new
            {
                error = "Client application not found."
            });
        }

        var result = await _mailService.SendAsync(request, application);

        if (!result.IsSuccess)
        {
            return BadRequest(new
            {
                error = result.Error
            });
        }

        return Ok(result.Data);
    }
}