using rs_ruralia.Shared.Models;
using System.Net.Http.Json;

namespace rs_ruralia.Web.Services;

public class RfqVendorDistributionsApiClient(HttpClient httpClient)
{
    public async Task<Result<IEnumerable<RfqVendorDistribution>>?> GetAllRfqVendorDistributionsAsync(CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync<IEnumerable<RfqVendorDistribution>>(
            () => httpClient.GetAsync("/rfq-vendor-distributions", cancellationToken),
            cancellationToken);
    }

    public async Task<Result<RfqVendorDistribution>?> GetRfqVendorDistributionByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync<RfqVendorDistribution>(
            () => httpClient.GetAsync($"/rfq-vendor-distributions/{id}", cancellationToken),
            cancellationToken);
    }

    public async Task<Result<IEnumerable<RfqVendorDistribution>>?> GetRfqVendorDistributionHistoryAsync(int id, CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync<IEnumerable<RfqVendorDistribution>>(
            () => httpClient.GetAsync($"/rfq-vendor-distributions/{id}/history", cancellationToken),
            cancellationToken);
    }

    public async Task<Result<RfqVendorDistribution>?> CreateRfqVendorDistributionAsync(RfqVendorDistribution entity, CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync<RfqVendorDistribution>(
            () => httpClient.PostAsJsonAsync("/rfq-vendor-distributions", entity, cancellationToken),
            cancellationToken);
    }

    public async Task<Result<RfqVendorDistribution>?> UpdateRfqVendorDistributionAsync(int id, RfqVendorDistribution entity, CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync<RfqVendorDistribution>(
            () => httpClient.PutAsJsonAsync($"/rfq-vendor-distributions/{id}", entity, cancellationToken),
            cancellationToken,
            successData: entity);
    }

    public async Task<Result?> DeleteRfqVendorDistributionAsync(int id, CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(
            () => httpClient.DeleteAsync($"/rfq-vendor-distributions/{id}", cancellationToken),
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
