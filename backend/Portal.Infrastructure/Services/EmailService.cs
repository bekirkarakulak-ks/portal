using Portal.Core.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Portal.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly string _smtpHost;
    private readonly int _smtpPort;
    private readonly string _smtpUser;
    private readonly string _smtpPassword;
    private readonly string _fromEmail;
    private readonly string _fromName;

    public EmailService(IConfiguration configuration)
    {
        _configuration = configuration;
        _smtpHost = Environment.GetEnvironmentVariable("SMTP_HOST") ?? configuration["Email:SmtpHost"] ?? "";
        _smtpPort = int.Parse(Environment.GetEnvironmentVariable("SMTP_PORT") ?? configuration["Email:SmtpPort"] ?? "587");
        _smtpUser = Environment.GetEnvironmentVariable("SMTP_USER") ?? configuration["Email:SmtpUser"] ?? "";
        _smtpPassword = Environment.GetEnvironmentVariable("SMTP_PASSWORD") ?? configuration["Email:SmtpPassword"] ?? "";
        _fromEmail = Environment.GetEnvironmentVariable("EMAIL_FROM") ?? configuration["Email:FromEmail"] ?? "noreply@portal.konyalisaat.com.tr";
        _fromName = Environment.GetEnvironmentVariable("EMAIL_FROM_NAME") ?? configuration["Email:FromName"] ?? "Portal";
    }

    public async Task SendVerificationEmailAsync(string email, string firstName, string token, string verificationUrl)
    {
        var fullVerificationUrl = $"{verificationUrl}?token={token}";

        // SMTP yapilandirma varsa gercek email gonder
        if (!string.IsNullOrEmpty(_smtpHost) && !string.IsNullOrEmpty(_smtpUser))
        {
            await SendEmailViaSmtpAsync(
                email,
                "Email Adresinizi Dogrulayin - Portal",
                BuildVerificationEmailBody(firstName, fullVerificationUrl)
            );
        }
        else
        {
            // SMTP yapilandirmasi yoksa konsola yazdir (development icin)
            Console.WriteLine("========================================");
            Console.WriteLine($"EMAIL VERIFICATION LINK:");
            Console.WriteLine($"To: {email}");
            Console.WriteLine($"Name: {firstName}");
            Console.WriteLine($"Link: {fullVerificationUrl}");
            Console.WriteLine($"Token: {token}");
            Console.WriteLine("========================================");
        }

        await Task.CompletedTask;
    }

    private async Task SendEmailViaSmtpAsync(string toEmail, string subject, string body)
    {
        try
        {
            using var client = new System.Net.Mail.SmtpClient(_smtpHost, _smtpPort);
            client.EnableSsl = true;
            client.Credentials = new System.Net.NetworkCredential(_smtpUser, _smtpPassword);

            var message = new System.Net.Mail.MailMessage
            {
                From = new System.Net.Mail.MailAddress(_fromEmail, _fromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            message.To.Add(toEmail);

            await client.SendMailAsync(message);
            Console.WriteLine($"Verification email sent to {toEmail}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to send email to {toEmail}: {ex.Message}");
            throw;
        }
    }

    public async Task SendPasswordResetEmailAsync(string email, string firstName, string token, string resetUrl)
    {
        var fullResetUrl = $"{resetUrl}?token={token}";

        // SMTP yapilandirma varsa gercek email gonder
        if (!string.IsNullOrEmpty(_smtpHost) && !string.IsNullOrEmpty(_smtpUser))
        {
            await SendEmailViaSmtpAsync(
                email,
                "Sifre Sifirlama - Portal",
                BuildPasswordResetEmailBody(firstName, fullResetUrl)
            );
        }
        else
        {
            // SMTP yapilandirmasi yoksa konsola yazdir (development icin)
            Console.WriteLine("========================================");
            Console.WriteLine($"PASSWORD RESET LINK:");
            Console.WriteLine($"To: {email}");
            Console.WriteLine($"Name: {firstName}");
            Console.WriteLine($"Link: {fullResetUrl}");
            Console.WriteLine($"Token: {token}");
            Console.WriteLine("========================================");
        }

        await Task.CompletedTask;
    }

    private string BuildPasswordResetEmailBody(string firstName, string resetUrl)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: #D72027; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 30px; background: #f9f9f9; }}
        .button {{ display: inline-block; background: #D72027; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
        .footer {{ padding: 20px; text-align: center; color: #666; font-size: 12px; }}
        .warning {{ background: #FEF3C7; padding: 15px; border-radius: 5px; margin: 15px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Portal</h1>
        </div>
        <div class='content'>
            <h2>Merhaba {firstName},</h2>
            <p>Sifrenizi sifirlamak icin bir talep aldik. Asagidaki butona tiklayarak yeni sifrenizi belirleyebilirsiniz.</p>
            <p style='text-align: center;'>
                <a href='{resetUrl}' class='button'>Sifremi Sifirla</a>
            </p>
            <p>Veya bu linki tarayiciniza kopyalayin:</p>
            <p style='word-break: break-all; background: #eee; padding: 10px;'>{resetUrl}</p>
            <div class='warning'>
                <strong>Onemli:</strong> Bu link 1 saat icinde gecerliliini yitirecektir.
            </div>
            <p>Eger bu istegi siz yapmadiyiniz, bu emaili gormezden gelebilirsiniz. Sifreniz degismeyecektir.</p>
        </div>
        <div class='footer'>
            <p>&copy; {DateTime.Now.Year} Portal. Tum haklar saklidir.</p>
        </div>
    </div>
</body>
</html>";
    }

    private string BuildVerificationEmailBody(string firstName, string verificationUrl)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: #1e3a5f; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 30px; background: #f9f9f9; }}
        .button {{ display: inline-block; background: #1e3a5f; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
        .footer {{ padding: 20px; text-align: center; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Portal</h1>
        </div>
        <div class='content'>
            <h2>Merhaba {firstName},</h2>
            <p>Portal'a kaydiniz tamamlanmak uzere. Email adresinizi dogrulamak icin asagidaki butona tiklayin.</p>
            <p style='text-align: center;'>
                <a href='{verificationUrl}' class='button'>Email Adresimi Dogrula</a>
            </p>
            <p>Veya bu linki tarayiciniza kopyalayin:</p>
            <p style='word-break: break-all; background: #eee; padding: 10px;'>{verificationUrl}</p>
            <p>Bu link 24 saat icinde gecerliliini yitirecektir.</p>
            <p>Eger bu kaydi siz yapmadiyiniz bu emaili gormezden gelebilirsiniz.</p>
        </div>
        <div class='footer'>
            <p>&copy; {DateTime.Now.Year} Portal. Tum haklar saklidir.</p>
        </div>
    </div>
</body>
</html>";
    }
}
