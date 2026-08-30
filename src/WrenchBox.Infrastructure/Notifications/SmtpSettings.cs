namespace WrenchBox.Infrastructure.Notifications;

public class SmtpSettings
{
    public const string SectionName = "Smtp";

    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 1025;
    public string User { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string From { get; set; } = "WrenchBox <noreply@wrenchbox.local>";
    public bool UseSsl { get; set; }
    public string PublicBaseUrl { get; set; } = "http://localhost:8080";
}

public class WebhookSettings
{
    public const string SectionName = "Webhook";

    public string Secret { get; set; } = "dev-webhook-secret";
}
