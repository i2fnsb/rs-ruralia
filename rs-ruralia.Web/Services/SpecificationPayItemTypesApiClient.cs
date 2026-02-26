using rs_ruralia.Shared.Models;
using System.Net.Http.Json;

namespace rs_ruralia.Web.Services;

public class SpecificationPayItemTypesApiClient(HttpClient httpClient)
{
    public async Task<Result<IEnumerable<SpecificationPayItemType>>?> GetAllSpecificationPayItemTypesAsync(CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync<IEnumerable<SpecificationPayItemType>>(
            () => httpClient.GetAsync("/specification-pay-item-types", cancellationToken),
            cancellationToken);
    }

    public async Task<Result<SpecificationPayItemType>?> GetSpecificationPayItemTypeByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync<SpecificationPayItemType>(
            () => httpClient.GetAsync($"/specification-pay-item-types/{id}", cancellationToken),
            cancellationToken);
    }

    public async Task<Result<IEnumerable<SpecificationPayItemType>>?> GetSpecificationPayItemTypeHistoryAsync(int id, CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync<IEnumerable<SpecificationPayItemType>>(
            () => httpClient.GetAsync($"/specification-pay-item-types/{id}/history", cancellationToken),
            cancellationToken);
    }

    public async Task<Result<SpecificationPayItemType>?> CreateSpecificationPayItemTypeAsync(SpecificationPayItemType entity, CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync<SpecificationPayItemType>(
            () => httpClient.PostAsJsonAsync("/specification-pay-item-types", entity, cancellationToken),
            cancellationToken);
    }

    public async Task<Result<SpecificationPayItemType>?> UpdateSpecificationPayItemTypeAsync(int id, SpecificationPayItemType entity, CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync<SpecificationPayItemType>(
            () => httpClient.PutAsJsonAsync($"/specification-pay-item-types/{id}", entity, cancellationToken),
            cancellationToken,
            successData: entity);
    }

    public async Task<Result?> DeleteSpecificationPayItemTypeAsync(int id, CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(
            () => httpClient.DeleteAsync($"/specification-pay-item-types/{id}", cancellationToken),
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
