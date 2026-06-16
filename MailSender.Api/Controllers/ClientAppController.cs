using MailSender.Application.DTOs.ClientApps;
using MailSender.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace MailSender.Api.Controllers;

[ApiController]
[Route("client-app")]
public class ClientAppController : ControllerBase
{
    private readonly ClientApplicationService _clientApplicationService;

    public ClientAppController(ClientApplicationService clientApplicationService)
    {
        _clientApplicationService = clientApplicationService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterClientAppRequest request)
    {
        var result = await _clientApplicationService.RegisterAsync(request);

        if (!result.IsSuccess)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                error = result.Error
            });
        }

        return Ok(result.Data);
    }
}