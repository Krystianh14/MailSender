namespace MailSender.Application.Interfaces;

public interface IRegistrationPasswordValidator
{
	bool IsValid(string password);

	string GetInvalidPasswordMessage();
}