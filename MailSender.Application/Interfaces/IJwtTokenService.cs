using MailSender.Domain.Entities;

namespace MailSender.Application.Interfaces;

public interface IJwtTokenService
{
    string GenerateToken(ClientApplication application);
}