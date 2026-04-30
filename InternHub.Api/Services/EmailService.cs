using System.Net;
using System.Net.Mail;

namespace InternHub.Api.Services;

public record EmailMessage(string To, string Subject, string Body);

public class EmailService(IConfiguration configuration, ILogger<EmailService> logger)
{
    public async Task<bool> SendAsync(EmailMessage message)
    {
        var section = configuration.GetSection("Email");
        var enabled = section.GetValue<bool>("Enabled");
        var host = section["Host"];
        var from = section["From"];

        if (!enabled || string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(from))
        {
            logger.LogInformation("Email not sent because SMTP is not configured. To={To}, Subject={Subject}", message.To, message.Subject);
            return false;
        }

        using var mail = new MailMessage(from, message.To, message.Subject, message.Body)
        {
            IsBodyHtml = true
        };

        using var client = new SmtpClient(host, section.GetValue<int>("Port"))
        {
            EnableSsl = section.GetValue<bool>("UseSsl"),
            Credentials = new NetworkCredential(section["Username"], section["Password"])
        };

        await client.SendMailAsync(mail);
        return true;
    }
}
