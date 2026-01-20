namespace Portal.Core.Interfaces;

public interface IEmailService
{
    Task SendVerificationEmailAsync(string email, string firstName, string token, string verificationUrl);
    Task SendPasswordResetEmailAsync(string email, string firstName, string token, string resetUrl);
}
