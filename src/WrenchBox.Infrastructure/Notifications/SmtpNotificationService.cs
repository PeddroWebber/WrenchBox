using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using WrenchBox.Application.Interfaces;
using WrenchBox.Domain.Enums;

namespace WrenchBox.Infrastructure.Notifications;

public class SmtpNotificationService : INotificationService, IBudgetNotificationService
{
    private readonly SmtpSettings _settings;
    private readonly ILogger<SmtpNotificationService> _logger;

    public SmtpNotificationService(IOptions<SmtpSettings> settings, ILogger<SmtpNotificationService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public Task<bool> SendBudgetApprovalRequestAsync(
        string customerEmail,
        string orderNumber,
        decimal totalAmount,
        string trackingToken,
        CancellationToken cancellationToken = default)
    {
        var approveUrl = $"{_settings.PublicBaseUrl.TrimEnd('/')}/api/v1/tracking/work-orders/decision?approved=true&token={Uri.EscapeDataString(trackingToken)}";
        var rejectUrl = $"{_settings.PublicBaseUrl.TrimEnd('/')}/api/v1/tracking/work-orders/decision?approved=false&token={Uri.EscapeDataString(trackingToken)}";

        var html = $"""
            <p>Olá,</p>
            <p>O orçamento da ordem de serviço <strong>{orderNumber}</strong> no valor de <strong>{totalAmount:C}</strong> está aguardando a sua decisão.</p>
            <p>
              <a href="{approveUrl}">Aprovar orçamento</a>
              &nbsp;|&nbsp;
              <a href="{rejectUrl}">Recusar orçamento</a>
            </p>
            <p>Token de acompanhamento: <code>{trackingToken}</code></p>
            """;

        return SendAsync(customerEmail, $"Orçamento da OS {orderNumber}", html, cancellationToken);
    }

    public Task<bool> SendStatusChangedAsync(
        string customerEmail,
        string orderNumber,
        WorkOrderStatus status,
        string statusLabel,
        CancellationToken cancellationToken = default)
    {
        var html = $"""
            <p>Olá,</p>
            <p>A ordem de serviço <strong>{orderNumber}</strong> foi atualizada para <strong>{statusLabel}</strong> ({status}).</p>
            """;

        return SendAsync(customerEmail, $"OS {orderNumber}: {statusLabel}", html, cancellationToken);
    }

    private async Task<bool> SendAsync(string to, string subject, string html, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_settings.Host))
        {
            _logger.LogInformation("SMTP host not configured. Skipping e-mail '{Subject}' to {To}.", subject, to);
            return true;
        }

        try
        {
            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(_settings.From));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;
            message.Body = new TextPart("html") { Text = html };

            using var client = new SmtpClient();
            var socketOptions = _settings.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None;
            await client.ConnectAsync(_settings.Host, _settings.Port, socketOptions, cancellationToken);

            if (!string.IsNullOrWhiteSpace(_settings.User))
                await client.AuthenticateAsync(_settings.User, _settings.Password, cancellationToken);

            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            _logger.LogInformation("E-mail '{Subject}' sent to {To}.", subject, to);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send e-mail '{Subject}' to {To}.", subject, to);
            return false;
        }
    }
}
