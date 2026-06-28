using MailSender.Application.Common;
using MailSender.Application.DTOs.ClientApps;
using MailSender.Application.Interfaces;
using MailSender.Domain.Entities;

namespace MailSender.Application.Services;

public class ClientApplicationService
{
    private readonly IClientApplicationRepository _clientApplicationRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRegistrationPasswordValidator _registrationPasswordValidator;

    public ClientApplicationService(
        IClientApplicationRepository clientApplicationRepository,
        IJwtTokenService jwtTokenService,
        IRegistrationPasswordValidator registrationPasswordValidator)
    {
        _clientApplicationRepository = clientApplicationRepository;
        _jwtTokenService = jwtTokenService;
        _registrationPasswordValidator = registrationPasswordValidator;
    }

    public async Task<ServiceResult<RegisterClientAppResponse>> RegisterAsync(RegisterClientAppRequest request)
    {
        if (!_registrationPasswordValidator.IsValid(request.Pass))
        {
            return ServiceResult<RegisterClientAppResponse>.Failure(
                _registrationPasswordValidator.GetInvalidPasswordMessage()
            );
        }

        var existingByAppId = await _clientApplicationRepository.GetByAppIdAsync(request.AppId);

        if (existingByAppId is not null)
        {
            return ServiceResult<RegisterClientAppResponse>.Failure(
                $"client app duplication. Existing {existingByAppId.AppId} {existingByAppId.AppName}"
            );
        }

        var existingByAppName = await _clientApplicationRepository.GetByAppNameAsync(request.AppName);

        if (existingByAppName is not null)
        {
            return ServiceResult<RegisterClientAppResponse>.Failure(
                $"client app duplication. Existing {existingByAppName.AppId} {existingByAppName.AppName}"
            );
        }

        var application = new ClientApplication(request.AppId, request.AppName);

        await _clientApplicationRepository.AddAsync(application);

        var token = _jwtTokenService.GenerateToken(application);

        var response = new RegisterClientAppResponse(
            application.AppId,
            application.AppName,
            token
        );

        return ServiceResult<RegisterClientAppResponse>.Success(response);
    }
}