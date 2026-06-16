namespace MailSender.Application.DTOs.ClientApps;

public record RegisterClientAppRequest(
	string AppId,
	string AppName,
	string Pass
);