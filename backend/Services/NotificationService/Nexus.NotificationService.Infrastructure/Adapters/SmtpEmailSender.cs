using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nexus.NotificationService.Domain.Repositories;
using System.Net;
using System.Net.Mail;

namespace Nexus.NotificationService.Infrastructure.Adapters;

/// <summary>
/// Gửi email thật qua SMTP (Gmail, Brevo, v.v.).
/// Config từ appsettings: Email:SmtpHost, Email:SmtpPort, Email:EnableSsl,
/// Email:Username, Email:Password, Email:FromEmail, Email:FromName.
///
/// Để test với yopmail: register với email @yopmail.com,
/// welcome email sẽ xuất hiện tại yopmail.com inbox.
/// </summary>
public class SmtpEmailSender : IEmailSender
{
    private readonly IConfiguration _config;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IConfiguration config, ILogger<SmtpEmailSender> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default)
    {
        var host     = _config["Email:SmtpHost"]!;
        var port     = int.Parse(_config["Email:SmtpPort"] ?? "587");
        var ssl      = bool.Parse(_config["Email:EnableSsl"] ?? "true");
        var user     = _config["Email:Username"]!;
        var pass     = _config["Email:Password"]!;
        var from     = _config["Email:FromEmail"] ?? user;
        var fromName = _config["Email:FromName"] ?? "ProjectNexus";

        using var smtp = new SmtpClient(host, port)
        {
            EnableSsl = ssl,
            Credentials = new NetworkCredential(user, pass),
            DeliveryMethod = SmtpDeliveryMethod.Network,
        };

        using var msg = new MailMessage
        {
            From = new MailAddress(from, fromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true,
        };
        msg.To.Add(toEmail);

        try
        {
            await smtp.SendMailAsync(msg, ct);
            _logger.LogInformation("[SMTP] Sent → {To} | Subject: {Subject}", toEmail, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SMTP] Failed → {To} | Subject: {Subject}", toEmail, subject);
            throw;
        }
    }
}
