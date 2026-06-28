using MailSender.Application.Interfaces;
using MailSender.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MailSender.Api.Controllers;

[ApiController]
[Authorize]
[Route("mail-log")]
public class MailLogController : ControllerBase
{
    private readonly MailLogService _mailLogService;
    private readonly IClientApplicationRepository _clientApplicationRepository;

    public MailLogController(
        MailLogService mailLogService,
        IClientApplicationRepository clientApplicationRepository)
    {
        _mailLogService = mailLogService;
        _clientApplicationRepository = clientApplicationRepository;
    }

    [HttpGet]
    public async Task<IActionResult> GetLogs()
    {
        var application = await GetCurrentApplicationAsync();

        if (application is null)
        {
            return Unauthorized(new
            {
                error = "Client application not found."
            });
        }

        var result = await _mailLogService.GetLogsAsync(application);

        return Ok(result.Data);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetLogById(Guid id)
    {
        var application = await GetCurrentApplicationAsync();

        if (application is null)
        {
            return Unauthorized(new
            {
                error = "Client application not found."
            });
        }

        var result = await _mailLogService.GetLogByIdAsync(id, application);

        if (!result.IsSuccess)
        {
            return NotFound(new
            {
                error = result.Error
            });
        }

        return Ok(result.Data);
    }

    private async Task<Domain.Entities.ClientApplication?> GetCurrentApplicationAsync()
{
    var applicationIdClaim = User.FindFirst("client_application_id")?.Value;

    if (!Guid.TryParse(applicationIdClaim, out var applicationId))
    {
        return null;
    }

    return await _clientApplicationRepository.GetByIdAsync(applicationId);
}
}