using rs_ruralia.Shared.Models;
using System.Net.Http.Json;

namespace rs_ruralia.Web.Services;

public class ServiceAreasApiClient(HttpClient httpClient)
{
    public async Task<Result<IEnumerable<ServiceArea>>?> GetAllServiceAreasAsync(CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync<IEnumerable<ServiceArea>>(
            () => httpClient.GetAsync("/service-areas", cancellationToken),
            cancellationToken);
    }

    public async Task<Result<ServiceArea>?> GetServiceAreaAsync(int id, CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync<ServiceArea>(
            () => httpClient.GetAsync($"/service-areas/{id}", cancellationToken),
            cancellationToken);
    }

    public async Task<Result<IEnumerable<ServiceArea>>?> GetServiceAreaHistoryAsync(int id, CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync<IEnumerable<ServiceArea>>(
            () => httpClient.GetAsync($"/service-areas/{id}/history", cancellationToken),
            cancellationToken);
    }

    public async Task<Result<ServiceArea>?> GetServiceAreaHistoryAtPointInTimeAsync(int id, DateTime asOfDate, CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync<ServiceArea>(
            () => httpClient.GetAsync($"/service-areas/{id}/history/at/{asOfDate:O}", cancellationToken),
            cancellationToken);
    }

    public async Task<Result<IEnumerable<ServiceArea>>?> GetServiceAreaHistoryBetweenAsync(int id, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync<IEnumerable<ServiceArea>>(
            () => httpClient.GetAsync($"/service-areas/{id}/history/between?startDate={startDate:O}&endDate={endDate:O}", cancellationToken),
            cancellationToken);
    }

    public async Task<Result<ServiceArea>?> CreateServiceAreaAsync(ServiceArea serviceArea, CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync<ServiceArea>(
            () => httpClient.PostAsJsonAsync("/service-areas", serviceArea, cancellationToken),
            cancellationToken);
    }

    public async Task<Result<ServiceArea>?> UpdateServiceAreaAsync(int id, ServiceArea serviceArea, CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync<ServiceArea>(
            () => httpClient.PutAsJsonAsync($"/service-areas/{id}", serviceArea, cancellationToken),
            cancellationToken,
            successData: serviceArea); // Use provided data since PUT returns NoContent
    }

    public async Task<Result?> DeleteServiceAreaAsync(int id, CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(
            () => httpClient.DeleteAsync($"/service-areas/{id}", cancellationToken),
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
                // Use provided data or deserialize from response
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