using rs_ruralia.Shared.Infrastructure;
using rs_ruralia.Shared.Models;
using System.Net.Http.Json;

namespace rs_ruralia.Web.Services;

public class OrdinancesApiClient(HttpClient httpClient)
{
    public async Task<Result<IEnumerable<Ordinance>>?> GetAllOrdinancesAsync(CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync<IEnumerable<Ordinance>>(
            () => httpClient.GetAsync("/ordinances", cancellationToken),
            cancellationToken);
    }

    public async Task<Result<Ordinance>?> GetOrdinanceAsync(int id, CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync<Ordinance>(
            () => httpClient.GetAsync($"/ordinances/{id}", cancellationToken),
            cancellationToken);
    }

    public async Task<Result<IEnumerable<Ordinance>>?> GetOrdinancesByServiceAreaAsync(int serviceAreaId, CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync<IEnumerable<Ordinance>>(
            () => httpClient.GetAsync($"/ordinances/service-area/{serviceAreaId}", cancellationToken),
            cancellationToken);
    }

    public async Task<Result<IEnumerable<Ordinance>>?> GetOrdinanceHistoryAsync(int id, CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync<IEnumerable<Ordinance>>(
            () => httpClient.GetAsync($"/ordinances/{id}/history", cancellationToken),
            cancellationToken);
    }

    public async Task<Result<Ordinance>?> CreateOrdinanceAsync(Ordinance ordinance, CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync<Ordinance>(
            () => httpClient.PostAsJsonAsync("/ordinances", ordinance, cancellationToken),
            cancellationToken);
    }

    public async Task<Result<Ordinance>?> UpdateOrdinanceAsync(int id, Ordinance ordinance, CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync<Ordinance>(
            () => httpClient.PutAsJsonAsync($"/ordinances/{id}", ordinance, cancellationToken),
            cancellationToken,
            successData: ordinance);
    }

    public async Task<Result?> DeleteOrdinanceAsync(int id, CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(
            () => httpClient.DeleteAsync($"/ordinances/{id}", cancellationToken),
            cancellationToken);
    }

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

    private async Task<Result<T>> ParseErrorAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var errorResult = await response.Content.ReadFromJsonAsync<ErrorResponse>(cancellationToken);
        return Result<T>.Failure(
            errorResult?.Errors ?? new[] { errorResult?.Error ?? $"Request failed with status code {response.StatusCode}" });
    }

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