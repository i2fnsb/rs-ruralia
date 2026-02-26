using rs_ruralia.Shared.Infrastructure;
using rs_ruralia.Shared.Models;
using System.Net.Http.Json;

namespace rs_ruralia.Web.Services;

public class OrdinanceTypesApiClient(HttpClient httpClient)
{
    public async Task<Result<IEnumerable<OrdinanceType>>?> GetAllOrdinanceTypesAsync(CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync<IEnumerable<OrdinanceType>>(
            () => httpClient.GetAsync("/ordinance-types", cancellationToken),
            cancellationToken);
    }

    public async Task<Result<OrdinanceType>?> GetOrdinanceTypeAsync(int id, CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync<OrdinanceType>(
            () => httpClient.GetAsync($"/ordinance-types/{id}", cancellationToken),
            cancellationToken);
    }

    public async Task<Result<IEnumerable<OrdinanceType>>?> GetOrdinanceTypeHistoryAsync(int id, CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync<IEnumerable<OrdinanceType>>(
            () => httpClient.GetAsync($"/ordinance-types/{id}/history", cancellationToken),
            cancellationToken);
    }

    public async Task<Result<OrdinanceType>?> CreateOrdinanceTypeAsync(OrdinanceType ordinanceType, CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync<OrdinanceType>(
            () => httpClient.PostAsJsonAsync("/ordinance-types", ordinanceType, cancellationToken),
            cancellationToken);
    }

    public async Task<Result<OrdinanceType>?> UpdateOrdinanceTypeAsync(int id, OrdinanceType ordinanceType, CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync<OrdinanceType>(
            () => httpClient.PutAsJsonAsync($"/ordinance-types/{id}", ordinanceType, cancellationToken),
            cancellationToken,
            successData: ordinanceType);
    }

    public async Task<Result?> DeleteOrdinanceTypeAsync(int id, CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(
            () => httpClient.DeleteAsync($"/ordinance-types/{id}", cancellationToken),
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