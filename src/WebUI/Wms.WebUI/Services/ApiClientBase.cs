using System.Net.Http.Json;
using Wms.WebUI.Bff;

namespace Wms.WebUI.Services;

// Base typed client untuk request ke gateway. Client gateway sudah menangani JWT dan resilience.
public abstract class ApiClientBase(IHttpClientFactory httpClientFactory)
{
    protected HttpClient Gateway => httpClientFactory.CreateClient(BffExtensions.GatewayClientName);

    protected async Task<ApiResult<PagedResult<T>>> GetPagedAsync<T>(string path, CancellationToken cancellationToken)
    {
        using var response = await Gateway.GetAsync(path, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return ApiResult<PagedResult<T>>.Fail(await ReadProblemAsync(response, cancellationToken));
        }

        var page = await response.Content.ReadFromJsonAsync<PagedResult<T>>(cancellationToken);
        return page is null
            ? ApiResult<PagedResult<T>>.Fail("Respons tak terbaca.")
            : ApiResult<PagedResult<T>>.Ok(page);
    }

    protected async Task<ApiResult<T>> GetOneAsync<T>(string path, CancellationToken cancellationToken)
    {
        using var response = await Gateway.GetAsync(path, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return ApiResult<T>.Fail(await ReadProblemAsync(response, cancellationToken));
        }

        var dto = await response.Content.ReadFromJsonAsync<T>(cancellationToken);
        return dto is null ? ApiResult<T>.Fail("Respons tak terbaca.") : ApiResult<T>.Ok(dto);
    }

    // Untuk endpoint yang langsung mengembalikan array.
    protected async Task<ApiResult<IReadOnlyList<T>>> GetListAsync<T>(string path, CancellationToken cancellationToken)
    {
        using var response = await Gateway.GetAsync(path, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return ApiResult<IReadOnlyList<T>>.Fail(await ReadProblemAsync(response, cancellationToken));
        }

        var list = await response.Content.ReadFromJsonAsync<List<T>>(cancellationToken);
        return ApiResult<IReadOnlyList<T>>.Ok(list ?? []);
    }

    protected async Task<ApiResult> PostJsonAsync(string path, object body, CancellationToken cancellationToken)
    {
        using var response = await Gateway.PostAsJsonAsync(path, body, cancellationToken);
        return response.IsSuccessStatusCode
            ? ApiResult.Ok()
            : ApiResult.Fail(await ReadProblemAsync(response, cancellationToken));
    }

    // Kirim POST lalu baca isi respons, misalnya ID hasil create untuk langsung dipilih.
    protected async Task<ApiResult<T>> PostReadAsync<T>(string path, object body, CancellationToken cancellationToken)
    {
        using var response = await Gateway.PostAsJsonAsync(path, body, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return ApiResult<T>.Fail(await ReadProblemAsync(response, cancellationToken));
        }

        var dto = await response.Content.ReadFromJsonAsync<T>(cancellationToken);
        return dto is null ? ApiResult<T>.Fail("Respons tak terbaca.") : ApiResult<T>.Ok(dto);
    }

    protected async Task<ApiResult> PostEmptyAsync(string path, CancellationToken cancellationToken)
    {
        using var response = await Gateway.PostAsync(path, content: null, cancellationToken);
        return response.IsSuccessStatusCode
            ? ApiResult.Ok()
            : ApiResult.Fail(await ReadProblemAsync(response, cancellationToken));
    }

    protected async Task<ApiResult> PutJsonAsync(string path, object body, CancellationToken cancellationToken)
    {
        using var response = await Gateway.PutAsJsonAsync(path, body, cancellationToken);
        return response.IsSuccessStatusCode
            ? ApiResult.Ok()
            : ApiResult.Fail(await ReadProblemAsync(response, cancellationToken));
    }

    protected async Task<ApiResult> DeleteAsync(string path, CancellationToken cancellationToken)
    {
        using var response = await Gateway.DeleteAsync(path, cancellationToken);
        return response.IsSuccessStatusCode
            ? ApiResult.Ok()
            : ApiResult.Fail(await ReadProblemAsync(response, cancellationToken));
    }

    // Kirim konten seperti multipart dan gunakan parser Problem Details yang sama.
    protected async Task<ApiResult<T>> PostContentReadAsync<T>(string path, HttpContent content, CancellationToken cancellationToken)
    {
        using var response = await Gateway.PostAsync(path, content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return ApiResult<T>.Fail(await ReadProblemAsync(response, cancellationToken));
        }

        var dto = await response.Content.ReadFromJsonAsync<T>(cancellationToken);
        return dto is null ? ApiResult<T>.Fail("Respons tak terbaca.") : ApiResult<T>.Ok(dto);
    }

    // Ambil pesan dari Problem Details agar UI bisa menampilkan penyebab error dengan jelas.
    private static async Task<string> ReadProblemAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<ProblemPayload>(cancellationToken);
            if (!string.IsNullOrWhiteSpace(problem?.Detail))
            {
                return problem.Detail;
            }

            if (!string.IsNullOrWhiteSpace(problem?.Title))
            {
                return problem.Title;
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (exception is HttpRequestException or System.Text.Json.JsonException)
        {
            // Respons bukan Problem Details yang valid, jadi gunakan pesan umum berdasarkan status HTTP.
        }

        return $"Gagal (HTTP {(int)response.StatusCode}).";
    }

    private sealed record ProblemPayload(string? Title, string? Detail, string? ErrorCode);
}
