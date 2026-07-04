using System.Text;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.BuildingBlocks.Domain.Results;
using Wms.Inbound.Application.Abstractions;
using Wms.Inbound.Application.Features.GetGRAttachmentDownloadUrl;
using Wms.Inbound.Application.Features.SoftDeleteGRAttachment;
using Wms.Inbound.Application.Features.UploadGRAttachment;
using Wms.Inbound.IntegrationTests.TestSupport;
using Xunit;

namespace Wms.Inbound.IntegrationTests;

// byte ke IObjectStore dan metadata (contentRef) satu commit
[Collection(PostgresCollection.Name)]
public sealed class AttachmentFlowTests(PostgresFixture postgres) : IAsyncLifetime
{
    private static readonly DateTimeOffset _now = new(2026, 7, 4, 9, 0, 0, TimeSpan.Zero);

    private ServiceProvider _provider = null!;

    private InMemoryObjectStore _objectStore = null!;

    public async Task InitializeAsync()
    {
        var connectionString = await postgres.CreateFreshDatabaseAsync();
        _objectStore = new InMemoryObjectStore();
        _provider = InboundTestHost.Build(connectionString, services =>
        {
            services.AddSingleton<IObjectStore>(_objectStore);
            services.AddSingleton<TimeProvider>(new FixedTimeProvider(_now));
        });
        await InboundTestHost.MigrateAsync(_provider);
    }

    public async Task DisposeAsync() => await _provider.DisposeAsync();

    [Fact]
    public async Task Upload_menyimpan_byte_dan_metadata_contentref()
    {
        var grId = await GoodsReceiptScenarios.CreateAsync(_provider, ("SKU-A", 1m));
        var bytes = Encoding.UTF8.GetBytes("%PDF-1.7 dokumen surat jalan");

        using var content = new MemoryStream(bytes);
        var uploaded = await PipelineRunner.SendAsync(
            _provider,
            new UploadGRAttachmentCommand(grId, "surat-jalan.pdf", "application/pdf", bytes.Length, content));

        uploaded.IsSuccess.Should().BeTrue(uploaded.IsFailure ? uploaded.Error.Message : string.Empty);

        var expectedPath = $"{grId:D}/{uploaded.Value:D}/surat-jalan.pdf";
        _objectStore.Objects.Should().ContainKey(expectedPath);
        _objectStore.Objects[expectedPath].Bytes.Should().Equal(bytes);
        _objectStore.Objects[expectedPath].ContentType.Should().Be("application/pdf");

        using var scope = _provider.CreateScope();
        var listed = await scope.ServiceProvider.GetRequiredService<IGRAttachmentReader>()
            .ListByGoodsReceiptAsync(grId);
        listed.Should().ContainSingle();
        listed[0].FileName.Should().Be("surat-jalan.pdf");
        listed[0].SizeBytes.Should().Be(bytes.Length);
        listed[0].UploadedAt.Should().Be(_now, "uploadedAt dari TimeProvider, domain bebas clock");
    }

    [Fact]
    public async Task Upload_contenttype_di_luar_whitelist_ditolak_tanpa_byte_tanpa_row()
    {
        var grId = await GoodsReceiptScenarios.CreateAsync(_provider, ("SKU-A", 1m));

        using var content = new MemoryStream(Encoding.UTF8.GetBytes("bukan pdf"));
        var uploaded = await PipelineRunner.SendAsync(
            _provider,
            new UploadGRAttachmentCommand(grId, "virus.exe", "application/octet-stream", 9, content));

        uploaded.IsFailure.Should().BeTrue();
        uploaded.Error.Code.Should().Be("gr_attachment.content_type_forbidden");
        _objectStore.Objects.Should().BeEmpty("invariant domain diuji sebelum byte menyentuh store");

        using var scope = _provider.CreateScope();
        (await scope.ServiceProvider.GetRequiredService<IGRAttachmentReader>().ListByGoodsReceiptAsync(grId))
            .Should().BeEmpty();
    }

    [Fact]
    public async Task Upload_oversize_ditolak_oleh_validator()
    {
        var grId = await GoodsReceiptScenarios.CreateAsync(_provider, ("SKU-A", 1m));

        using var content = new MemoryStream([1, 2, 3]);
        var uploaded = await PipelineRunner.SendAsync(
            _provider,
            new UploadGRAttachmentCommand(grId, "big.pdf", "application/pdf", (50L * 1024 * 1024) + 1, content));

        uploaded.IsFailure.Should().BeTrue();
        uploaded.ErrorType.Should().Be(ResultErrorType.Validation);
    }

    [Fact]
    public async Task Upload_ke_gr_yang_tidak_ada_notfound()
    {
        using var content = new MemoryStream([1]);
        var uploaded = await PipelineRunner.SendAsync(
            _provider,
            new UploadGRAttachmentCommand(Guid.NewGuid(), "a.pdf", "application/pdf", 1, content));

        uploaded.IsFailure.Should().BeTrue();
        uploaded.Error.Code.Should().Be("goods_receipt.not_found");
    }

    [Fact]
    public async Task Download_mengembalikan_presigned_url_bukan_byte()
    {
        var (grId, attachmentId) = await UploadSampleAsync();

        var downloadUrl = await PipelineRunner.SendAsync(
            _provider,
            new GetGRAttachmentDownloadUrlQuery(grId, attachmentId));

        downloadUrl.IsSuccess.Should().BeTrue();
        downloadUrl.Value.Url.Query.Should().Contain("sig=", "valet key path-scoped + signed, bukan stream byte");
        downloadUrl.Value.Url.AbsolutePath.Should().Contain($"{grId:D}/{attachmentId:D}");
        downloadUrl.Value.ExpiresAt.Should().Be(_now.AddMinutes(15));
    }

    [Fact]
    public async Task SoftDelete_menyembunyikan_dari_listing_dan_download()
    {
        var (grId, attachmentId) = await UploadSampleAsync();

        var deleted = await PipelineRunner.SendAsync(
            _provider,
            new SoftDeleteGRAttachmentCommand(grId, attachmentId));
        deleted.IsSuccess.Should().BeTrue();

        using var scope = _provider.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<IGRAttachmentReader>();
        (await reader.ListByGoodsReceiptAsync(grId)).Should().BeEmpty();

        var all = await reader.ListByGoodsReceiptAsync(grId, includeInactive: true);
        all.Should().ContainSingle();
        all[0].IsActive.Should().BeFalse();

        (await PipelineRunner.SendAsync(_provider, new GetGRAttachmentDownloadUrlQuery(grId, attachmentId)))
            .ErrorType.Should().Be(ResultErrorType.NotFound);

        (await PipelineRunner.SendAsync(_provider, new SoftDeleteGRAttachmentCommand(grId, attachmentId)))
            .ErrorType.Should().Be(ResultErrorType.NotFound);
    }

    [Fact]
    public async Task SoftDelete_gr_mismatch_notfound_tanpa_bocor()
    {
        var (_, attachmentId) = await UploadSampleAsync();
        var otherGrId = await GoodsReceiptScenarios.CreateAsync(_provider, ("SKU-B", 1m));

        var deleted = await PipelineRunner.SendAsync(
            _provider,
            new SoftDeleteGRAttachmentCommand(otherGrId, attachmentId));

        deleted.ErrorType.Should().Be(ResultErrorType.NotFound);
    }

    private async Task<(Guid GrId, Guid AttachmentId)> UploadSampleAsync()
    {
        var grId = await GoodsReceiptScenarios.CreateAsync(_provider, ("SKU-A", 1m));
        using var content = new MemoryStream(Encoding.UTF8.GetBytes("%PDF-1.7"));
        var uploaded = await PipelineRunner.SendAsync(
            _provider,
            new UploadGRAttachmentCommand(grId, "pod.pdf", "application/pdf", 8, content));
        uploaded.IsSuccess.Should().BeTrue();
        return (grId, uploaded.Value);
    }
}
