namespace Wms.BuildingBlocks.Application.Abstractions.Ports;

// notif email: ACS Azure, SendGrid GCP, log stub Local.
public interface IEmailSender
{
    Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default);
}
