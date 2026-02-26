using rs_ruralia.Shared.Models;
using System.Net.Http.Json;

namespace rs_ruralia.Web.Services;

public class RfqsApiClient(HttpClient httpClient)
{
    public async Task<Result<IEnumerable<Rfq>>?> GetAllRfqsAsync(CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync<IEnumerable<Rfq>>(
            () => httpClient.GetAsync("/rfqs", cancellationToken),
            cancellationToken);
    }

    public async Task<Result<Rfq>?> GetRfqByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync<Rfq>(
            () => httpClient.GetAsync($"/rfqs/{id}", cancellationToken),
            cancellationToken);
    }

    public async Task<Result<IEnumerable<Rfq>>?> GetRfqHistoryAsync(int id, CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync<IEnumerable<Rfq>>(
            () => httpClient.GetAsync($"/rfqs/{id}/history", cancellationToken),
            cancellationToken);
    }

    public async Task<Result<Rfq>?> CreateRfqAsync(Rfq entity, CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync<Rfq>(
            () => httpClient.PostAsJsonAsync("/rfqs", entity, cancellationToken),
            cancellationToken);
    }

    public async Task<Result<Rfq>?> UpdateRfqAsync(int id, Rfq entity, CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync<Rfq>(
            () => httpClient.PutAsJsonAsync($"/rfqs/{id}", entity, cancellationToken),
            cancellationToken,
            successData: entity);
    }

    public async Task<Result?> DeleteRfqAsync(int id, CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(
            () => httpClient.DeleteAsync($"/rfqs/{id}", cancellationToken),
            cancellationToken);
    }

    // Generic helper for requests that return data
    private async Task<Result<T>?> ExecuteAsync<T>(
        Func<Task<HttpResponseMessage>> request,
        CancellationToken cancellationToken,
        T? successData = default)
    {
        try
        {
            var response = await request();

            if (response.IsSuccessStatusCode)
            {
                var data = successData ?? await response.Content.ReadFromJsonAsync<T>(cancellationToken);
                return data != null
                    ? Result<T>.Success(data)
                    : Result<T>.Failure("No data returned");
            }

            return await ParseErrorAsync<T>(response, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            return Result<T>.Failure($"Network error: {ex.Message}");
        }
        catch (Exception ex)
        {
            return Result<T>.Failure($"Unexpected error: {ex.Message}");
        }
    }

    // Helper for requests that don't return data (like DELETE)
    private async Task<Result?> ExecuteAsync(
        Func<Task<HttpResponseMessage>> request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await request();

            if (response.IsSuccessStatusCode)
            {
                return Result.Success();
            }

            return await ParseErrorAsync(response, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            return Result.Failure($"Network error: {ex.Message}");
        }
        catch (Exception ex)
        {
            return Result.Failure($"Unexpected error: {ex.Message}");
        }
    }

    // Parse error response for generic Result<T>
    private async Task<Result<T>> ParseErrorAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var errorResult = await response.Content.ReadFromJsonAsync<ErrorResponse>(cancellationToken);
        return Result<T>.Failure(
            errorResult?.Errors ?? new[] { errorResult?.Error ?? $"Request failed with status code {response.StatusCode}" });
    }

    // Parse error response for non-generic Result
    private async Task<Result> ParseErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var errorResult = await response.Content.ReadFromJsonAsync<ErrorResponse>(cancellationToken);
        return Result.Failure(
            errorResult?.Errors ?? new[] { errorResult?.Error ?? $"Request failed with status code {response.StatusCode}" });
    }

    private class ErrorResponse
    {
        public string? Error { get; set; }
        public string[]? Errors { get; set; }
    }
}
