using Jobuler.Application.Common;

namespace Jobuler.Infrastructure.Notifications;

/// <summary>
/// Sends group invitations via WhatsApp using TwilioWhatsAppSender.
/// Uses a friendly Hebrew message with the invite link.
/// </summary>
public class WhatsAppInvitationSender : IInvitationSender
{
    private readonly TwilioWhatsAppSender _twilio;

    public WhatsAppInvitationSender(TwilioWhatsAppSender twilio)
    {
        _twilio = twilio;
    }

    public Task SendInvitationAsync(
        string contact, string channel, string inviteUrl, string personName,
        CancellationToken ct = default)
    {
        if (channel != "whatsapp") return Task.CompletedTask;

        var message = $"שלום {personName}! 👋\n\n" +
                      $"הוזמנת להצטרף לקבוצה ב-Shifter.\n\n" +
                      $"לחץ על הקישור כדי לאשר את ההזמנה:\n{inviteUrl}\n\n" +
                      $"הקישור תקף ל-7 ימים.";

        return _twilio.SendRawAsync(contact, message, ct);
    }
}
