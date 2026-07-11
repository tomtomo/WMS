using AwesomeAssertions;
using Azure;
using Azure.Communication.Email;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Wms.Platform.Azure.Notifications;
using Xunit;

namespace Wms.Platform.Azure.ParityTests;

// Channel email untuk Azure memakai ACS. Pengiriman yang sukses wajib membawa providerMessageId, sedangkan yang gagal dilempar
// agar worker notifikasi yang melakukan retry lalu dead letter, bukan core.
public sealed class AcsEmailSenderTests
{
    private const string Recipient = "gudang@contoh.co.id";

    [Fact]
    public async Task Send_posts_the_message_from_the_configured_sender_address()
    {
        var client = Substitute.For<EmailClient>();
        client.SendAsync(Arg.Any<WaitUntil>(), Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => new EmailSendOperation("acs-operation-1", client));
        var sender = NewSender(client);

        await sender.SendAsync(Recipient, "GR dikonfirmasi", "GR-001 selesai diterima");

        await client.Received(1).SendAsync(
            WaitUntil.Started,
            Arg.Is<EmailMessage>(message =>
                message.SenderAddress == "DoNotReply@wms.example.net"
                && message.Recipients.To[0].Address == Recipient
                && message.Content.Subject == "GR dikonfirmasi"
                && message.Content.PlainText == "GR-001 selesai diterima"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Send_fails_when_acs_returns_no_operation_id()
    {
        var client = Substitute.For<EmailClient>();
        client.SendAsync(Arg.Any<WaitUntil>(), Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => new EmailSendOperation(" ", client));

        var send = () => NewSender(client).SendAsync(Recipient, "Judul", "Isi");

        await send.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Send_propagates_provider_failure_so_the_worker_can_retry()
    {
        var client = Substitute.For<EmailClient>();
        client.SendAsync(Arg.Any<WaitUntil>(), Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new RequestFailedException(503, "ACS sedang sibuk"));

        var send = () => NewSender(client).SendAsync(Recipient, "Judul", "Isi");

        await send.Should().ThrowAsync<RequestFailedException>();
    }

    [Fact]
    public async Task Send_rejects_a_blank_recipient()
    {
        var send = () => NewSender(Substitute.For<EmailClient>()).SendAsync(" ", "Judul", "Isi");

        await send.Should().ThrowAsync<ArgumentException>();
    }

    private static AcsEmailSender NewSender(EmailClient client) =>
        new(
            client,
            Options.Create(new AcsEmailOptions { SenderAddress = "DoNotReply@wms.example.net" }),
            NullLogger<AcsEmailSender>.Instance);
}
