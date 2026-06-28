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
    var applicationIdClaim = User.FindFirst("client_application_id")?.Value;

    if (!Guid.TryParse(applicationIdClaim, out var applicationId))
    {
        return Unauthorized(new
        {
            error = "Invalid token. Missing or invalid client_application_id claim."
        });
    }

    var application = await _clientApplicationRepository.GetByIdAsync(applicationId);

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