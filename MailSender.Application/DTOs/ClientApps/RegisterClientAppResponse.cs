namespace MailSender.Application.DTOs.ClientApps;

public record RegisterClientAppResponse(
	string AppId,
	string AppName,
	string Key
);