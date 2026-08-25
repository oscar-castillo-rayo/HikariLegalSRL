using HikariLegalSRL.Services;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using MimeKit;

namespace HikariLegalSRL.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly ILogger<EmailSender> _logger;
        private readonly SmtpSettings _smtp;

        public EmailSender(ILogger<EmailSender> logger,
            IOptions<SmtpSettings> smtpOptions)
        {
            _logger = logger;
            _smtp = smtpOptions.Value;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var message = new MimeMessage();

            var fromName = string.IsNullOrWhiteSpace(_smtp.FromName) ? _smtp.User : _smtp.FromName;
            var fromEmail = string.IsNullOrWhiteSpace(_smtp.FromEmail) ? _smtp.User : _smtp.FromEmail;

            message.From.Add(new MailboxAddress(fromName, fromEmail));
            message.To.Add(MailboxAddress.Parse(email));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = htmlMessage };
            message.Body = bodyBuilder.ToMessageBody();

            try
            {
                _logger.LogInformation("SMTP config: Host={Host}, Port={Port}, User={User}, UseSsl={UseSsl}", _smtp.Host, _smtp.Port, _smtp.User, _smtp.UseSsl);

                using var client = new SmtpClient();

                var useSsl = _smtp.UseSsl;
                await client.ConnectAsync(_smtp.Host, _smtp.Port, useSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls);

                if (!string.IsNullOrWhiteSpace(_smtp.User))
                {
                    await client.AuthenticateAsync(_smtp.User, _smtp.Password);
                }

                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("Correo enviado a {Email}", email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enviando correo a {Email}", email);
            }
        }
    }
}