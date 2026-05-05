using Jobuler.Application.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Jobuler.Infrastructure.Email;

/// <summary>
/// Sends emails via SendGrid.
/// Requires configuration:
///   SendGrid:ApiKey       — your SendGrid API key
///   SendGrid:FromEmail    — verified sender email address
///   SendGrid:FromName     — display name for the sender (default: "Shifter")
///
/// If ApiKey is not configured, falls back to NoOpEmailSender behaviour (logs + no-op).
/// </summary>
public class SendGridEmailSender : IEmailSender
{
    private readonly ILogger<SendGridEmailSender> _logger;
    private readonly string? _apiKey;
    private readonly string _fromEmail;
    private readonly string _fromName;

    public SendGridEmailSender(IConfiguration config, ILogger<SendGridEmailSender> logger)
    {
        _logger = logger;
        _apiKey = config["SendGrid:ApiKey"];
        _fromEmail = config["SendGrid:FromEmail"] ?? "noreply@shifter.app";
        _fromName = config["SendGrid:FromName"] ?? "Shifter";
    }

    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            _logger.LogWarning(
                "SendGrid:ApiKey not configured — email not sent to={To} subject={Subject}",
                to, subject);
            return;
        }

        var client = new SendGridClient(_apiKey);
        var from = new EmailAddress(_fromEmail, _fromName);
        var toAddress = new EmailAddress(to);

        var msg = MailHelper.CreateSingleEmail(from, toAddress, subject, null, htmlBody);

        var response = await client.SendEmailAsync(msg, ct);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Body.ReadAsStringAsync(ct);
            _logger.LogError(
                "SendGrid delivery failed: status={Status} to={To} subject={Subject} body={Body}",
                (int)response.StatusCode, to, subject, body);
        }
        else
        {
            _logger.LogInformation(
                "Email sent via SendGrid: to={To} subject={Subject}",
                to, subject);
        }
    }
}
